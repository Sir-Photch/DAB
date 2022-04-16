using DAB.Discord.Abstracts;
using DAB.Discord.Enums;
using Discord;
using Discord.WebSocket;
using System.Reflection;

namespace DAB.Discord.Commands
{
    internal static class SlashCommandFactory
    {
        private static IServiceProvider? _serviceProvider;

        internal static void Initlaize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        internal static async Task ApplyAllCommandsAsync(this DiscordSocketClient client)
        {
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
            var builder = new SlashCommandBuilder().ApplySlashCommandDetails(commandType);

            if (builder is null)
                return null;

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
                case SlashCommandType.INVALID:
                    return null;
                default:
                    throw new NotImplementedException();
            }

            return builder.Build();
        }

        internal static SlashCommandType? FromString(string? str)
        {
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

            if (handler is null)
                throw new NotSupportedException($"ServiceProvider did not contain instance of {nameof(AbstractHandlerModule<SlashCommand>)}");

            handler.Context = command;
            await handler.HandleAsync();
        }

        private static SlashCommandBuilder? ApplySlashCommandDetails(this SlashCommandBuilder builder, SlashCommandType commandType)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            var details = commandType.GetType().GetField(commandType.ToString())?.GetCustomAttribute<SlashCommandDetailsAttribute>();
            if (details is null)
            {
                if (commandType is SlashCommandType.INVALID)
                {
                    return null;
                }
                else throw new NotImplementedException();
            }

            return builder.WithName(details.CommandName).WithDescription(details.Description);
        }
    }
}
