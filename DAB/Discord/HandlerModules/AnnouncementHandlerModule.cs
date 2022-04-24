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
    private static IUserDataSink GetSink() => GlobalServiceProvider.GetService<IUserDataSink>();
    private static int GetAnnouncementDurationMaxMs() => GlobalServiceProvider.GetService<ConfigRoot>().Bot.ChimeDurationMaxMs;

    internal override async Task<bool> HandleAsync()
    {
        if (Context?.Command is null || Context.CommandType is not SlashCommandType.chime)
            return false;

        ChimeCommand cmd;
        try
        {
            cmd = new(Context);
        }
        catch (ArgumentException ae)
        {
            Log.Write(ERR, ae, "Could not parse commandContext for ChimeCommand");
            await Context.FollowupAsync("Whoops! An unexpected error has occurred. Contact your admin!", ephemeral: true);
            return false;
        }

        bool continueHandling = await cmd.ValidateRequestedModeAsync() && cmd.RequestedMode is not ChimeCommand.Mode.Clear;

        if (continueHandling)
        {
            using Stream fromWeb = await cmd.GetAudioFileStreamAsync();
            await LoadAudioFileAsync(fromWeb, cmd.Context);
        }

        return true;
    }

    private static async Task LoadAudioFileAsync(Stream stream, ISlashCommandInteraction interaction)
    {
#if DEBUG
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (interaction is null) throw new ArgumentNullException(nameof(interaction));
#endif
        using MemoryStream ms = new();
        await stream.CopyToAsync(ms);

        bool validData = await CheckMetadataAsync(ms, interaction);
        if (!validData)
            return;

        try
        {
            using Stream pcmStream = await AudioEncoder.EncodePCMAsync(ms);
            await GetSink().SaveAsync(interaction.User.Id, pcmStream);
        }
        catch (Exception e)
        {
            Log.Write(FTL, e, "AudioEncoding error");
            await interaction.FollowupAsync("Crikey! An unexpected error has occurred. Contact your admin!", ephemeral: true);
            return;
        }

        await interaction.FollowupAsync("Your announcement has been saved!", ephemeral: true);
    }

    private static async Task<bool> CheckMetadataAsync(Stream data, ISlashCommandInteraction interaction, CancellationToken token = default)
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
            Log.Write(WRN, e, "ffmpeg-error: probably not an audio-file");
            await interaction.FollowupAsync("Hm, is this an audio-file?", ephemeral: true);
            return false;
        }

        int maxDurationMs = GetAnnouncementDurationMaxMs();

        if (metadata.Duration.TotalMilliseconds >= maxDurationMs)
        {
            await interaction.FollowupAsync($"Sorry, your announcement can't be any longer than {maxDurationMs / 1000.0:F1} seconds", ephemeral: true);
            return false;
        }

        return true;
    }
}
