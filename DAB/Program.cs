using DAB.Data.Interfaces;
using DAB.Data.Sinks;
using DAB.Discord;
using DAB.Discord.Abstracts;
using DAB.Discord.Audio;
using DAB.Discord.Commands;
using DAB.Discord.HandlerModules;
using DAB.Configuration;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using DAB.Configuration.Exceptions;

const string DAB_LOGO = "######     #    ######\n#     #   # #   #     #\n#     #  #   #  #     #\n#     # #     # ######\n#     # ####### #     #\n#     # #     # #     #\n######  #     # ######\n";

static void Cleanup(Stopwatch sw)
{
    Log.Write(INF, "Session lasted {uptime}", sw.Elapsed);
    Log.Flush();
}

// ------------------- APPLICATION START ------------------

Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine(DAB_LOGO);
Console.ForegroundColor = ConsoleColor.Blue;
Console.WriteLine("==> press 'q' to quit.\n");
Console.ResetColor();

Stopwatch stopwatch = Stopwatch.StartNew();

try
{
    Config.EnsureConfigExists(@throw: true);
}
catch (NotConfiguredException nce)
{
    Log.Write(FTL, "==> Config did not exist!");
    Log.Write(FTL, "==> It was created at {path}", Config.Path);
    Log.Write(FTL, nce, "==> Make sure to add your discord-api-key!");
    Cleanup(stopwatch);
    return 1;
}

ConfigRoot configRoot;
try
{
    configRoot = Config.GetRoot();
}
catch (NotConfiguredException nce)
{
    Log.Write(FTL, nce, "Fatal error, could not read config!");
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
    Log.Write(level, e, "[{source}] | {message}", source.PadRight(8), message);
    return Task.CompletedTask;
};

IServiceProvider serviceProvider = new ServiceCollection()
                                      .AddSingleton(client)
                                      .AddSingleton(AudioClientManager.Instance)
                                      .AddSingleton<AbstractHandlerModule<SlashCommand>>(provider => new AnnouncementHandlerModule(provider))
                                      .AddSingleton<IUserDataSink>(new FileSystemSink(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "userdata")))
                                      .AddSingleton<ConfigRoot>(configRoot)
                                      .BuildServiceProvider();

DiscordAnnouncementService discordCommandService = new(serviceProvider);

Log.Write(INF, "Bot startup: {startupTime}", DateTime.Now);

// I guess you go: start -> then login; and the other way around for: logout -> stop
await client.StartAsync();
await client.LoginAsync(TokenType.Bot, configRoot.Discord.ApiKey);

while (Console.ReadKey(intercept: true).KeyChar != 'q') ;

await client.LogoutAsync();
await client.StopAsync();

Cleanup(stopwatch);
return 0;
