using DAB.Data.Interfaces;
using DAB.Data.Sinks;
using DAB.Discord;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

static void Cleanup(Stopwatch sw)
{
    Log.Write(INF, "Bot shutdown: {Uptime}", sw.Elapsed);
    Log.Flush();
}

Log.Write(INF, "Bot startup: {StartupTime}", DateTime.Now);
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

await using AudioClientManager audioClientManager = AudioClientManager.Instance;
FileSystemSink debugSink = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "userdata"));
IServiceProvider serviceProvider = new ServiceCollection()
                                      .AddSingleton<IUserDataSink>(debugSink)
                                      .BuildServiceProvider();

await using DiscordCommandService discordCommandService = new(serviceProvider, audioClientManager, debugSink);

await discordCommandService.InitializeAsync();
await discordCommandService.StartAsync(discordKeys.ApiKey);

Console.ReadKey();

await discordCommandService.StopAsync();

Cleanup(stopwatch);
return 0;
