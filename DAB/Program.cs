using DAB.Data.Interfaces;
using DAB.Data.Sinks;
using DAB.Discord;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

const string DAB_LOGO = "######     #    ######\n#     #   # #   #     #\n#     #  #   #  #     #\n#     # #     # ######\n#     # ####### #     #\n#     # #     # #     #\n######  #     # ######\n";

static void Cleanup(Stopwatch sw)
{
    Log.Write(INF, "Bot shutdown: {Uptime}", sw.Elapsed);
    Log.Flush();
}

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

await using DiscordSocketClient client = new(new()
{
    GatewayIntents = GatewayIntents.AllUnprivileged
                   & ~(GatewayIntents.GuildScheduledEvents | GatewayIntents.GuildInvites)
});
await using AudioClientManager audioClientManager = AudioClientManager.Instance;
FileSystemSink debugSink = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "userdata"));
IServiceProvider serviceProvider = new ServiceCollection()
                                      .AddSingleton<DiscordSocketClient>(client)
                                      .AddSingleton<IUserDataSink>(debugSink)
                                      .BuildServiceProvider();

await using DiscordCommandService discordCommandService = new(serviceProvider, audioClientManager, client, debugSink);

Log.Write(INF, "Bot startup: {StartupTime}", DateTime.Now);


await discordCommandService.InitializeAsync();
await discordCommandService.StartAsync(discordKeys.ApiKey);

while (Console.ReadKey(intercept: true).KeyChar != 'q') ;

await discordCommandService.StopAsync();

Cleanup(stopwatch);
return 0;
