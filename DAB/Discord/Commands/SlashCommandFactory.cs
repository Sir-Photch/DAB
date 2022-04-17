using DAB.Discord.Abstracts;
using DAB.Discord.Enums;
using Discord;
using Discord.WebSocket;
using System.Reflection;

namespace DAB.Discord.Commands;

internal static class SlashCommandFactory
{
    private static IServiceProvider? _serviceProvider;

    internal static void Initlaize(IServiceProvider serviceProvider)
    {
#if DEBUG
        if (serviceProvider is null) throw new ArgumentNullException(nameof(serviceProvider));
#endif
        _serviceProvider = serviceProvider;
    }

    internal static async Task ApplyAllCommandsAsync(this DiscordSocketClient client)
    {
#if DEBUG
        if (client is null) throw new ArgumentNullException(nameof(client));
        if (client.ConnectionState is not ConnectionState.Connected) throw new InvalidOperationException($"{nameof(client)} is not connected");
#endif
        await Task.WhenAll(
            Enum.GetValues<SlashCommandType>().Select(x => Task.Run(async () =>
            {
                SlashCommandProperties? command = CreateCommand(x);
                if (command is null)
                    return;
                await client.CreateGlobalApplicationCommandAsync(command);
            }))
        );
    }

    internal static SlashCommandProperties? CreateCommand(SlashCommandType commandType)
    {
        if (commandType is SlashCommandType.INVALID)
            return null;

        var builder = new SlashCommandBuilder().ApplySlashCommandDetails(commandType);

        switch (commandType)
        {
            case SlashCommandType.SET_CHIME:
                var options = new SlashCommandOptionBuilder().WithName("audio-file")
                                                             .WithDescription("audio-file to add")
                                                             .WithRequired(true)
                                                             .WithType(ApplicationCommandOptionType.Attachment);
                builder = builder.AddOption(options);
                break;
            case SlashCommandType.CLEAR_CHIME:
                break;
#if DEBUG
            default:
                throw new NotImplementedException();
#endif
        }

        return builder.Build();
    }

    internal static SlashCommandType? FromString(string? str)
    {
        if (str is null)
            return null;

        var obj = typeof(SlashCommandType).GetFields()
                                      .Where(x => x.GetCustomAttribute<SlashCommandDetailsAttribute>()?.CommandName == str)
                                      .FirstOrDefault()?.GetValue(null);

        if (obj is null)
            return null;

        return (SlashCommandType)obj;
    }

    internal static async Task HandleCommandAsync(SlashCommand command)
    {
        var handler = _serviceProvider?.GetService(typeof(AbstractHandlerModule<SlashCommand>)) as AbstractHandlerModule<SlashCommand>;

#if DEBUG
        if (handler is null) throw new NotSupportedException($"ServiceProvider did not contain instance of {nameof(AbstractHandlerModule<SlashCommand>)}");
#endif

        handler.Context = command;
        await handler.HandleAsync();
    }

    private static SlashCommandBuilder ApplySlashCommandDetails(this SlashCommandBuilder builder, SlashCommandType commandType)
    {
#if DEBUG
        if (builder is null) throw new ArgumentNullException(nameof(builder));
#endif

        var details = commandType.GetType()
                                 .GetField(commandType.ToString())
                                 ?.GetCustomAttribute<SlashCommandDetailsAttribute>();

#if DEBUG
        if (details is null) throw new NotImplementedException();
#endif

        return builder.WithName(details.CommandName).WithDescription(details.Description);
    }
}
