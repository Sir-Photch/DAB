using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using DAB.Discord;
using DAB.Data.Sinks;
using DAB.Discord.Audio;
using DAB.Discord.Commands;
using DAB.Discord.Abstracts;
using DAB.Discord.HandlerModules;
using DAB.Data.Interfaces;

const string DAB_LOGO = "######     #    ######\n#     #   # #   #     #\n#     #  #   #  #     #\n#     # #     # ######\n#     # ####### #     #\n#     # #     # #     #\n######  #     # ######\n";

static void Cleanup(Stopwatch sw)
{
    Log.Write(INF, "Bot shutdown after: {uptime}", sw.Elapsed);
    Log.Flush();
}

// ------------------- APPLICATION START ------------------

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine(DAB_LOGO);
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine("==> press 'q' to quit.\n");
Console.ResetColor();

Stopwatch stopwatch = Stopwatch.StartNew();

Config.DiscordKeys discordKeys;
try
{
    discordKeys = Config.Get<Config.DiscordKeys>();
}
catch (InvalidOperationException ioe)
{
    Log.Write(ERR, ioe, "Discord-keys are not configured in {configpath}", Config.CONFIG_FILENAME);
    Cleanup(stopwatch);
    return 1;
}

DiscordSocketClient client = new(new()
{
    GatewayIntents = GatewayIntents.AllUnprivileged
                   & ~(GatewayIntents.GuildScheduledEvents | GatewayIntents.GuildInvites),
    UseInteractionSnowflakeDate = false
});
client.Log += msg =>
{
    (Log.Level level, string source, string message, Exception? e) = msg;
    Log.Write(level, e, "[{source}] | {message}", source.PadRight(7), message);
    return Task.CompletedTask;
};

IServiceProvider serviceProvider = new ServiceCollection()
                                      .AddSingleton(client)
                                      .AddSingleton(AudioClientManager.Instance)
                                      .AddSingleton<AbstractHandlerModule<SlashCommand>>(provider => new AnnouncementHandlerModule(provider))
                                      .AddSingleton<IUserDataSink>(new FileSystemSink(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "userdata")))
                                      .BuildServiceProvider();

DiscordAnnouncementService discordCommandService = new(serviceProvider);

Log.Write(INF, "Bot startup: {startupTime}", DateTime.Now);

// I guess you go: start -> then login; and the other way around for: logout -> stop
await client.StartAsync();
await client.LoginAsync(TokenType.Bot, discordKeys.ApiKey);

while (Console.ReadKey(intercept: true).KeyChar != 'q') ;

await client.LogoutAsync();
await client.StopAsync();

Cleanup(stopwatch);
return 0;
