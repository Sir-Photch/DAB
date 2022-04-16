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

await using DiscordSocketClient client = new(new()
{
    GatewayIntents = GatewayIntents.AllUnprivileged
                   & ~(GatewayIntents.GuildScheduledEvents | GatewayIntents.GuildInvites),
    UseInteractionSnowflakeDate = false
});

await using AudioClientManager audioClientManager = AudioClientManager.Instance;

FileSystemSink debugSink = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "userdata"));
AbstractHandlerModule<SlashCommand> handlerModule = new AnnouncementHandlerModule(debugSink);

IServiceProvider serviceProvider = new ServiceCollection()
                                      .AddSingleton<DiscordSocketClient>(client)
                                      .AddSingleton<AbstractHandlerModule<SlashCommand>>(handlerModule)
                                      .BuildServiceProvider();

await using DiscordAnnouncementService discordCommandService = new(serviceProvider, audioClientManager, client, debugSink);

Log.Write(INF, "Bot startup: {startupTime}", DateTime.Now);


await discordCommandService.InitializeAsync();
await discordCommandService.StartAsync(discordKeys.ApiKey);

while (Console.ReadKey(intercept: true).KeyChar != 'q') ;

await discordCommandService.StopAsync();

Cleanup(stopwatch);
return 0;
