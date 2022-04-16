using DAB.Audio;
using DAB.Data;
using DAB.Data.Interfaces;
using DAB.Discord.Abstracts;
using DAB.Discord.Enums;
using Discord;
using Microsoft.VisualStudio.Threading;
using System.Runtime.CompilerServices;
using TagLib;
using File = TagLib.File;

namespace DAB.Discord;

internal class AnnouncementHandlerModule : AbstractHandlerModule<SlashCommand>
{
    private readonly IUserDataSink _sink;

    internal AnnouncementHandlerModule(IUserDataSink sink)
    {
        _sink = sink;
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

        await _sink.ClearAsync(Context.User.Id);
        await context.FollowupAsync("Success! I won't cheer you on anymore :'(");
    }

    private async Task LoadAudioFileAsync(IAttachment attachment, ISlashCommandInteraction interaction)
    {
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

        using Stream pcmStream = await PCMAudioEncoder.Encode(ms);
        await _sink.SaveAsync(interaction.User.Id, pcmStream);
        await interaction.FollowupAsync("Your announcement has been saved!", ephemeral: true);
    }
}
