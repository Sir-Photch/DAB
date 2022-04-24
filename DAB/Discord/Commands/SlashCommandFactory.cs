using DAB.Discord.Enums;
using DAB.Discord.HandlerModules;
using Discord;
using Discord.WebSocket;
using Microsoft.VisualStudio.Threading;

namespace DAB.Discord.Commands;

internal static class SlashCommandFactory
{
    internal static async Task OverwriteCommandsAsync(this DiscordSocketClient client)
    {
#if DEBUG
        if (client is null) throw new ArgumentNullException(nameof(client));
        if (client.ConnectionState is not ConnectionState.Connected) throw new InvalidOperationException($"{nameof(client)} is not connected");
#endif
        var commands = Enum.GetValues<SlashCommandType>().Select(cmd => CreateCommand(cmd)).Where(x => x is not null);
        await client.BulkOverwriteGlobalApplicationCommandsAsync(commands.ToArray());
    }

    internal static SlashCommandProperties? CreateCommand(SlashCommandType commandType)
    {
        if (commandType is SlashCommandType.INVALID)
            return null;

        return commandType switch
        {
            SlashCommandType.chime => ChimeCommand.CreateCommand(),
            _ => throw new NotImplementedException()
        };
    }

    internal static SlashCommandType? FromString(string? str)
    {
        var matches = Enum.GetValues<SlashCommandType>().Where(t => t.ToString() == str);

        // avoid .FirstOrDefault(), default of enum is not null
        if (matches.Any())
            return matches.First();

        return null;
    }

    internal static async Task HandleCommandAsync(SlashCommand command)
    {
        var handlers = GlobalServiceProvider.GetService<HandlerCollection<SlashCommand>>();
        await command.DeferAsync(ephemeral: true);
        handlers.HandleAsync(command).ContinueWith(t =>
        {
            if (t.IsFaulted) Log.Write(FTL, t.Exception, "Unexpected exception in commandHandling");
            if (!t.Result) Log.Write(ERR, "Could not handle SlashCommand {type}", command.CommandType);
        },
        TaskScheduler.Default).Forget();
    }
}
