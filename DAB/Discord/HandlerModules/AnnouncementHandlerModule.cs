using Microsoft.VisualStudio.Threading;
using Discord;
using TagLib;
using File = TagLib.File;
using DAB.Data;
using DAB.Util.Audio;
using DAB.Discord.Enums;
using DAB.Discord.Commands;
using DAB.Data.Interfaces;
using DAB.Discord.Abstracts;

namespace DAB.Discord.HandlerModules;

internal class AnnouncementHandlerModule : AbstractHandlerModule<SlashCommand>
{
    private readonly IUserDataSink _sink;

    internal AnnouncementHandlerModule(IServiceProvider serviceProvider)
    {
#if DEBUG
        if (serviceProvider is null) throw new ArgumentNullException(nameof(serviceProvider));
#endif

        _sink = serviceProvider.GetService(typeof(IUserDataSink)) as IUserDataSink
                ?? throw new NotSupportedException($"{nameof(IUserDataSink)} missing from {nameof(serviceProvider)}");
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

        if (_sink.DataSizeCapBytes > 0 && attachment.Size > _sink.DataSizeCapBytes)
        {
            await context.FollowupAsync($"Whoa, this file is to big. It can be at most {_sink.DataSizeCapBytes} bytes", ephemeral: true);
            return;
        }

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

        using HttpClient client = new();
        using Stream data = await client.GetStreamAsync(attachment.Url);

        using MemoryStream ms = new();
        await data.CopyToAsync(ms);
        StreamFileAbstraction fileAbstraction = new(ms, attachment.Filename);

        try
        {
            File taggedFile = File.Create(fileAbstraction);

            if (taggedFile.PossiblyCorrupt)
            {
                await interaction.FollowupAsync("This file is corrupt!", ephemeral: true);
                return;
            }

            var audioCodecs = taggedFile.Properties.Codecs.Where(c => c is IAudioCodec or ILosslessAudioCodec);
            if (!audioCodecs.Any())
            {
                await interaction.FollowupAsync("Ha! This is not an audio-file!", ephemeral: true);
                return;
            }
        }
        catch (UnsupportedFormatException ufe)
        {
            Log.Write(DBG, ufe, "User provided unsupported format!");
            await interaction.FollowupAsync("This file is not supported :(", ephemeral: true);

            return;
        }
        catch (Exception e)
        {
            Log.Write(ERR, e, "Unexpected exception in {method}!", nameof(LoadAudioFileAsync));
            await interaction.FollowupAsync("Bollocks! An unexpected error occurred. Contact your admin!", ephemeral: true);
            return;
        }

        using Stream pcmStream = await PCMAudioEncoder.EncodeAsync(ms);
        await _sink.SaveAsync(interaction.User.Id, pcmStream);
        await interaction.FollowupAsync("Your announcement has been saved!", ephemeral: true);
    }
}
