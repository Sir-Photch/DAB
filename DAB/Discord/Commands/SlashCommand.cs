using DAB.Discord.Enums;
using Discord;
using Discord.WebSocket;

namespace DAB.Discord.Commands;

internal class SlashCommand : ISlashCommandInteraction, IApplicationCommandInteraction, IDiscordInteraction, ISnowflakeEntity, IEntity<ulong>
{
    internal SlashCommandType CommandType { get; init; }
    internal SocketSlashCommand Command { get; init; }

    #region interfaces

    public IApplicationCommandInteractionData Data => ((ISlashCommandInteraction)Command).Data;

    public ulong Id => ((IDiscordInteraction)Command).Id;

    public InteractionType Type => ((IDiscordInteraction)Command).Type;

    public string Token => ((IDiscordInteraction)Command).Token;

    public int Version => ((IDiscordInteraction)Command).Version;

    public bool HasResponded => ((IDiscordInteraction)Command).HasResponded;

    public IUser User => ((IDiscordInteraction)Command).User;

    public string UserLocale => ((IDiscordInteraction)Command).UserLocale;

    public string GuildLocale => ((IDiscordInteraction)Command).GuildLocale;

    public bool IsDMInteraction => ((IDiscordInteraction)Command).IsDMInteraction;

    public DateTimeOffset CreatedAt => ((ISnowflakeEntity)Command).CreatedAt;

    IDiscordInteractionData IDiscordInteraction.Data => ((IDiscordInteraction)Command).Data;

    public Task DeferAsync(bool ephemeral = false, RequestOptions? options = null)
    {
        return ((IDiscordInteraction)Command).DeferAsync(ephemeral, options);
    }

    public Task DeleteOriginalResponseAsync(RequestOptions? options = null)
    {
        return ((IDiscordInteraction)Command).DeleteOriginalResponseAsync(options);
    }

    public Task<IUserMessage> FollowupAsync(string? text = null, Embed[]? embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null, MessageComponent components = null, Embed embed = null, RequestOptions options = null)
    {
        return ((IDiscordInteraction)Command).FollowupAsync(text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
    }

    public Task<IUserMessage> FollowupWithFilesAsync(IEnumerable<FileAttachment> attachments, string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null, MessageComponent components = null, Embed embed = null, RequestOptions options = null)
    {
        return ((IDiscordInteraction)Command).FollowupWithFilesAsync(attachments, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
    }

    public Task<IUserMessage> GetOriginalResponseAsync(RequestOptions options = null)
    {
        return ((IDiscordInteraction)Command).GetOriginalResponseAsync(options);
    }

    public Task<IUserMessage> ModifyOriginalResponseAsync(Action<MessageProperties> func, RequestOptions options = null)
    {
        return ((IDiscordInteraction)Command).ModifyOriginalResponseAsync(func, options);
    }

    public Task RespondAsync(string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null, MessageComponent components = null, Embed embed = null, RequestOptions options = null)
    {
        return ((IDiscordInteraction)Command).RespondAsync(text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
    }

    public Task RespondWithFilesAsync(IEnumerable<FileAttachment> attachments, string text = null, Embed[] embeds = null, bool isTTS = false, bool ephemeral = false, AllowedMentions allowedMentions = null, MessageComponent components = null, Embed embed = null, RequestOptions options = null)
    {
        return ((IDiscordInteraction)Command).RespondWithFilesAsync(attachments, text, embeds, isTTS, ephemeral, allowedMentions, components, embed, options);
    }

    public Task RespondWithModalAsync(Modal modal, RequestOptions options = null)
    {
        return ((IDiscordInteraction)Command).RespondWithModalAsync(modal, options);
    }

    #endregion
}
