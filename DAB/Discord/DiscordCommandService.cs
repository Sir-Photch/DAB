using DAB.Data.Interfaces;
using DAB.Data.Sinks;
using DAB.Discord.Audio;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.VisualStudio.Threading;
using System.Collections.Concurrent;
using System.Reflection;

namespace DAB.Discord;

internal class DiscordCommandService
{
    #region fields

    private bool _disposed = false;
    private readonly DiscordSocketClient _socketClient;
    private readonly CommandService _commandService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAnnouncementSink _announcementSink;
    private readonly AudioService _audioService;

    private readonly SemaphoreSlim _sendAudioSemaphore = new(1);
    private readonly ConcurrentQueue<Stream> _sendAudioQueue = new();

    #endregion

    internal DiscordCommandService(
        IServiceProvider serviceProvider,
        IAnnouncementSink? announcementSink = null,
        DiscordSocketClient? socketClient = null,
        CommandService? commandService = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _announcementSink = announcementSink ?? new MemorySink();
        _socketClient = socketClient ?? new();
        _commandService = commandService ?? new();
        _audioService = new AudioService();
    }

    internal string CommandPrefix { get; init; } = "!";

    #region methods

    internal async Task InitializeAsync()
    {
        _socketClient.Log += OnClientLogAsync;
        _socketClient.MessageReceived += OnClientMessageReceivedAsync;
        _socketClient.UserVoiceStateUpdated += OnClientUserVoiceStateUpdatedAsync;

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

    private async Task OnClientUserVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState prevState, SocketVoiceState newState)
    {
        if (user.IsWebhook || user.IsBot || newState.VoiceChannel is null ||
            newState.IsSuppressed || newState.IsMuted ||
            !await _announcementSink.UserHasDataAsync(user.Id))
            return;

        Stream userData = await _announcementSink.LoadAsync(user.Id);
        _sendAudioQueue.Enqueue(userData);

        StartAudio(newState.VoiceChannel);
    }

    private void StartAudio(IVoiceChannel channel) => Task.Run(async () =>
    {
        await _audioService.JoinAudioAsync(channel.Guild, channel);

        while (_sendAudioQueue.TryDequeue(out Stream? result) && result is not null)
        {
            result.Position = 0;
            await _audioService.SendAudioAsync(channel.Guild, result);
        }

        await _audioService.LeaveAudio(channel.Guild);
    });

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
        (_announcementSink as IDisposable)?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await _socketClient.DisposeAsync();
        (_commandService as IDisposable)?.Dispose();
        (_announcementSink as IDisposable)?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    #endregion
}
