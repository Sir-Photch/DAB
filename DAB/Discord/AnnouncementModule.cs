using DAB.Audio;
using DAB.Data;
using DAB.Data.Interfaces;
using Discord;
using Discord.Commands;
using Microsoft.VisualStudio.Threading;
using TagLib;

namespace DAB.Discord;

public class AnnouncementModule : ModuleBase<ICommandContext>
{
    private readonly IAnnouncementSink _sink;

    public AnnouncementModule(IAnnouncementSink sink)
    {
        _sink = sink;
    }

    [Command("clear", RunMode = RunMode.Async)]
    public async Task ClearCommandAsync()
    {
        if (!await _sink.UserHasDataAsync(Context.User.Id))
        {
            await Context.ReplyEphemeralMessageAsync("You have no announcement!");
            return;
        }

        await _sink.ClearAsync(Context.User.Id);
    }

    [Command("set", RunMode = RunMode.Async)]
    public async Task SetCommandAsync()
    {
        var attachments = Context.Message.Attachments;
        IAttachment attachment = null;

        if (attachments.Count == 0)
        {
            // TODO make ephemeral
            await ReplyAsync("You did not attach any files!");
            return;
        }

        if (_sink.DataSizeCapBytes > 0 && (attachment = attachments.First()).Size > _sink.DataSizeCapBytes)
        {
            await ReplyAsync($"Whoa, this file is to big. It can be at most {_sink.DataSizeCapBytes} bytes");
            return;
        }

        if (attachment is null)
        {
            Log.Write(ERR, "Unexpected error in {method}: {reference} was null!", nameof(SetCommandAsync), nameof(attachment));
            await ReplyAsync($"Bollocks! Unexpected error occurred. Contact your admin!");
            return;
        }

        Task.Run(() => LoadAudioFileAsync(attachment, Context)).Forget();
    }

    private async Task LoadAudioFileAsync(IAttachment attachment, ICommandContext originalContext)
    {
        using HttpClient client = new();
        using Stream data = await client.GetStreamAsync(attachment.Url);

        using MemoryStream ms = new();
        await data.CopyToAsync(ms);
        StreamFileAbstraction fileAbstraction = new(ms, attachment.Filename);

        try
        {
            TagLib.File taggedFile = TagLib.File.Create(fileAbstraction);

            if (taggedFile.PossiblyCorrupt)
            {
                await ReplyAsync("This file is corrupt!");
                return;
            }

            var audioCodecs = taggedFile.Properties.Codecs.Where(c => c is IAudioCodec or ILosslessAudioCodec);
            if (!audioCodecs.Any())
            {
                await ReplyAsync("Ha! This is not an audio-file!");
                return;
            }
        }
        catch (UnsupportedFormatException ufe)
        {
            Log.Write(DBG, ufe, "User provided unsupported format!");
            await ReplyAsync("Ha! This file is not supported.");
            return;
        }
        catch (Exception e)
        {
            Log.Write(ERR, e, "Unexpected exception in {method}!", nameof(LoadAudioFileAsync));
            await ReplyAsync("Bollocks! An unexpected error occurred. Contact your admin!");
            return;
        }

        using Stream pcmStream = await PCMAudioEncoder.Encode(ms);
        await _sink.SaveAsync(originalContext.User.Id, pcmStream);
        await ReplyAsync("Your announcement has been saved!");
    }
}
