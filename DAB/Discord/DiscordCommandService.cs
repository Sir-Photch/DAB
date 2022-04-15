using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Reflection;

namespace DAB.Discord;

internal class DiscordCommandService : IDisposable, IAsyncDisposable
{
    #region fields

    private bool _disposed = false;
    private readonly DiscordSocketClient _socketClient;
    private readonly CommandService _commandService;
    private readonly IServiceProvider _serviceProvider;

    #endregion

    internal DiscordCommandService(
        IServiceProvider serviceProvider,
        DiscordSocketClient? socketClient = null, 
        CommandService? commandService = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider)); ;
        _socketClient = socketClient ?? new();
        _commandService = commandService ?? new();        
    }

    internal string CommandPrefix { get; init; } = "!";

    #region methods

    internal async Task InitializeAsync()
    {
        _socketClient.Log += OnClientLogAsync;
        _socketClient.MessageReceived += OnClientMessageReceivedAsync;
        _socketClient.UserVoiceStateUpdated += OnClientUserVoiceStateUpdated;

        await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
    }

    internal async Task StartAsync(string token)
    {
        await _socketClient.LoginAsync(TokenType.Bot, token);
        await _socketClient.StartAsync();
    }

    internal async Task StopAsync()
    {
        await _socketClient.StopAsync();
    }

    #endregion

    #region event-subscribers

    private async Task OnClientUserVoiceStateUpdated(SocketUser arg1, SocketVoiceState arg2, SocketVoiceState arg3)
    {
        return;
    }

    private async Task OnClientMessageReceivedAsync(SocketMessage arg)
    {
        if (arg is not SocketUserMessage msg)
            return;

        int argPos = 0;

        if (!(msg.HasStringPrefix(CommandPrefix, ref argPos) ||
              msg.HasMentionPrefix(_socketClient.CurrentUser, ref argPos)) ||
            msg.Author.IsBot)
            return;

        SocketCommandContext context = new(_socketClient, msg);

        await _commandService.ExecuteAsync(context, argPos, _serviceProvider);
    }

    private Task OnClientLogAsync(LogMessage arg)
    {
        (Log.Level level, string source, string message, Exception? e) = arg;
        Log.Write(level, e, "Discord-client: [{source}] | {message}", source, message);
        return Task.CompletedTask;
    }

    #endregion

    #region IDisposable, IAsyncDisposable

    public void Dispose()
    {
        if (_disposed)
            return;

        _socketClient.Dispose();
        (_commandService as IDisposable)?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await _socketClient.DisposeAsync();
        (_commandService as IDisposable)?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}
