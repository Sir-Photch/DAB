using DAB.Configuration;
using DAB.Data.Interfaces;
using DAB.Discord.Abstracts;
using DAB.Discord.Audio;
using DAB.Discord.Commands;
using DAB.Discord.Enums;
using Discord;
using FFMpegCore;
using Microsoft.VisualStudio.Threading;

namespace DAB.Discord.HandlerModules;

internal class AnnouncementHandlerModule : AbstractHandlerModule<SlashCommand>
{
    private readonly IUserDataSink _sink;
    private readonly int _announcementDurationMaxMs;
    private readonly int _announcementFileSizeMaxB;
    private readonly HttpClient _httpClient;

    internal AnnouncementHandlerModule(IServiceProvider serviceProvider)
    {
#if DEBUG
        if (serviceProvider is null) throw new ArgumentNullException(nameof(serviceProvider));
#endif

        _sink = serviceProvider.GetService(typeof(IUserDataSink)) as IUserDataSink
              ?? throw new NotSupportedException($"{nameof(IUserDataSink)} missing from {nameof(serviceProvider)}");

        _httpClient = serviceProvider.GetService(typeof(HttpClient)) as HttpClient
                    ?? throw new NotSupportedException($"{nameof(HttpClient)} missing from {nameof(serviceProvider)}");

        ConfigRoot? cfg = serviceProvider.GetService(typeof(ConfigRoot)) as ConfigRoot;

        _announcementDurationMaxMs = cfg?.Bot.ChimeDurationMaxMs ?? 10_000;
        _announcementFileSizeMaxB = (cfg?.Bot.ChimeFilesizeMaxKb ?? 5_000) * 1_000;
    }

    internal override async Task HandleAsync()
    {
        if (Context?.Command is null)
            return;

        switch (Context.CommandType)
        {
            case SlashCommandType.SET_CHIME:
                await SetChimeAsync();
                break;

            case SlashCommandType.CLEAR_CHIME:
                await ClearChimeAsync();
                break;

            case SlashCommandType.INVALID:
            default:
                break;
        }
    }

    private async Task SetChimeAsync()
    {
        SlashCommand? context = Context;

        var option = context?.Data.Options.FirstOrDefault();

        if (option?.Value is not IAttachment attachment)
            return;

#pragma warning disable CS8602, CS8604 // method will return if context is null

        if (attachment is null)
        {
            Log.Write(ERR, "Unexpected error in {method}: {reference} was null!", nameof(SetChimeAsync), nameof(attachment));
            await context.FollowupAsync($"Bollocks! Unexpected error occurred. Contact your admin!", ephemeral: true);
            return;
        }

        Task.Run(() => LoadAudioFileAsync(attachment, context)).Forget();

#pragma warning restore CS8602, CS8604
    }

    private async Task ClearChimeAsync()
    {
        SlashCommand? context = Context;

        if (context?.User is null)
            return;

        if (!await _sink.UserHasDataAsync(context.User.Id))
        {
            await context.FollowupAsync("You had no chime set", ephemeral: true);
            return;
        }

        await _sink.ClearAsync(context.User.Id);
        await context.FollowupAsync("Success! I won't cheer you on anymore :'(", ephemeral: true);
    }

    private async Task LoadAudioFileAsync(IAttachment attachment, ISlashCommandInteraction interaction)
    {
#if DEBUG
        if (attachment is null) throw new ArgumentNullException(nameof(attachment));
        if (interaction is null) throw new ArgumentNullException(nameof(interaction));
#endif

        if (_announcementFileSizeMaxB > 0 &&
            attachment.Size > _announcementFileSizeMaxB)
        {
            await interaction.FollowupAsync($"Sorry, your announcement can't be bigger than {_announcementFileSizeMaxB / 1000.0:F2} KB");
            return;
        }

        using Stream data = await _httpClient.GetStreamAsync(attachment.Url);

        using MemoryStream ms = new();
        await data.CopyToAsync(ms);

        bool validData = await CheckMetadataAsync(ms, interaction);
        if (!validData)
            return;

        try
        {
            using Stream pcmStream = await AudioEncoder.EncodePCMAsync(ms);
            await _sink.SaveAsync(interaction.User.Id, pcmStream);
        }
        catch (Exception e)
        {
            Log.Write(FTL, e, "AudioEncoding error");
            await interaction.FollowupAsync("Crikey! An unexpected error has occurred. Contact your admin!", ephemeral: true);
            return;
        }

        await interaction.FollowupAsync("Your announcement has been saved!", ephemeral: true);
    }

    private async Task<bool> CheckMetadataAsync(Stream data, ISlashCommandInteraction interaction, CancellationToken token = default)
    {
#if DEBUG
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (interaction is null) throw new ArgumentNullException(nameof(interaction));
#endif

        if (data.CanSeek)
            data.Seek(0, SeekOrigin.Begin);

        IMediaAnalysis metadata;
        try
        {
            metadata = await FFProbe.AnalyseAsync(data, cancellationToken: token);
        }
        catch (Exception e)
        {
            Log.Write(ERR, e, "Could not analyse data!");
            await interaction.RespondAsync("Weird! That did not work...", ephemeral: true);
            return false;
        }

        if (metadata.Duration.TotalMilliseconds >= _announcementDurationMaxMs)
        {
            await interaction.FollowupAsync($"Sorry, your announcement can't be any longer than {_announcementDurationMaxMs / 1000.0:F1} seconds", ephemeral: true);
            return false;
        }

        return true;
    }
}
