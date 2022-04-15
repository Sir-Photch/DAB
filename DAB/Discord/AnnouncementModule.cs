using DAB.Data.Interfaces;
using Discord;
using Discord.Commands;

namespace DAB.Discord;

public class AnnouncementModule : ModuleBase<ICommandContext>
{
    private readonly IAnnouncementSink _sink;

    public AnnouncementModule(IAnnouncementSink sink)
    {
        _sink = sink;
    }

    [Command("set", RunMode = RunMode.Async)]
    public async Task SetCommandAsync()
    {
        var attachments = Context.Message.Attachments;
        IAttachment? attachment = null;
        string? errorMessage = null;

        if (attachments.Count == 0)
            errorMessage = "No attachment!";
        else if ((attachment = attachments.First()).Size >= 2_000_000) // TODO make this configurable
            errorMessage = "Filesize is limited to 2MB";
        else if (Path.GetFileNameWithoutExtension(attachment.Filename) != ".mp3")
            errorMessage = "only mp3-files are supported";

        if (errorMessage is not null)
        {
            MessageReference reference = new(Context.Message.Id, Context.Channel.Id, Context.Guild.Id);
            await Context.Channel.SendMessageAsync(errorMessage, messageReference: reference, flags: MessageFlags.Ephemeral);
            return;
        }

        // TODO enqueue attachment download, but on another thread...
    }
}
