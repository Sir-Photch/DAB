using Discord;
using Discord.Commands;

namespace DAB.Discord
{
    internal static class Extensions
    {
        internal static async Task<IUserMessage> ReplyEphemeralMessageAsync(this ICommandContext context, string message)
        {
            return await context.Channel.SendMessageAsync(text: message,
                                                          messageReference: new MessageReference(context.Message.Id, context.Channel.Id, context.Guild.Id),
                                                          flags: MessageFlags.Ephemeral);
        }
    }
}
