using DAB.Discord;
using DAB.Discord.Audio;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

void Cleanup(Stopwatch sw)
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

IServiceProvider serviceProvider = new ServiceCollection()
                                      .AddSingleton<AudioService>()
                                      .BuildServiceProvider();

await using DiscordCommandService discordCommandService = new(serviceProvider);

await discordCommandService.InitializeAsync();
await discordCommandService.StartAsync(discordKeys.ApiKey);

Console.ReadKey();

await discordCommandService.StopAsync();

Cleanup(stopwatch);
return 0;
