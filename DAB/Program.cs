using DAB.Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

void Cleanup(Stopwatch sw)
{
    Log.Write(INF, "Bot shutdown: {Uptime}", sw.Elapsed);
    Log.Flush();
}

async Task OnMessageReceivedAsync(SocketMessage arg, DiscordSocketClient client, CommandService service, IServiceProvider provider)
{
    if (arg is not SocketUserMessage msg)
        return;

    int argPos = 0;

    if (!(msg.HasCharPrefix('!', ref argPos) ||
          msg.HasMentionPrefix(client.CurrentUser, ref argPos)) ||
        msg.Author.IsBot)
        return;

    SocketCommandContext context = new(client, msg);

    await service.ExecuteAsync(context, argPos, provider);
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

await using DiscordSocketClient client = new();
using CommandService service = new();
IServiceProvider serviceProvider = new ServiceCollection().AddSingleton<AudioService>().BuildServiceProvider();
client.Log += msg => Task.Run(() =>
{
    (Log.Level level, string source, string message, Exception? e) = msg;
    Log.Write(level, e, "Discord-client: [{source}] | {message}", source, message);
});
client.MessageReceived += msg => OnMessageReceivedAsync(msg, client, service, serviceProvider);

await client.LoginAsync(Discord.TokenType.Bot, discordKeys.ApiKey);
await client.StartAsync();
await service.AddModuleAsync<AudioModule>(serviceProvider);

Console.ReadKey();

await client.StopAsync();

Cleanup(stopwatch);
return 0;
