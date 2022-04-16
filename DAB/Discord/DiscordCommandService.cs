using DAB.Data.Interfaces;
using DAB.Data.Sinks;
using Discord;
using Discord.Audio;
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
    private readonly IUserDataSink _announcementSink;
    private readonly AudioClientManager _audioClientManager;

    private readonly ConcurrentDictionary<ulong, ConcurrentQueue<Stream>> _sendAudioQueue = new();

    #endregion

    internal DiscordCommandService(
        IServiceProvider serviceProvider,
        AudioClientManager audioClientManager,
        DiscordSocketClient socketClient,
        IUserDataSink? announcementSink = null,        
        CommandService? commandService = null)
    {
        _audioClientManager = audioClientManager ?? throw new ArgumentNullException(nameof(audioClientManager));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _socketClient = socketClient ?? throw new ArgumentNullException(nameof(socketClient));
        _announcementSink = announcementSink ?? new MemorySink();
        _commandService = commandService ?? new();
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

        //ulong channelId = newState.VoiceChannel.Id;

        //(_sendAudioQueue.ContainsKey(channelId)
        //    ? _sendAudioQueue[channelId]
        //    : (_sendAudioQueue[channelId] = new())).Enqueue(userData);

        StartAudio(newState.VoiceChannel, user.Id);
    }

    private void StartAudio(IVoiceChannel channel, ulong userId) => Task.Run(async () =>
    {
        Stream? userData = await _announcementSink.LoadAsync(userId);
        if (userData is null)
            return;

        IAudioClient client = await _audioClientManager.GetClientAsync(channel);

        try
        {
            userData.Position = 0L;
            await client.SendPCMEncodedAudioAsync(userData);

            // TODO clean up this mess!!!

            //if (!_sendAudioQueue.TryGetValue(channel.Id, out ConcurrentQueue<Stream>? audioQueue) ||
            //audioQueue is null)
            //    return;

            //while (audioQueue.TryDequeue(out Stream? stream) && stream is not null)
            //{
            //    stream.Position = 0;
            //    await client.SendPCMEncodedAudioAsync(stream);
            //}

            //_sendAudioQueue.TryRemove(channel.Id, out _);
        }
        finally
        {
            await client.StopAsync();
            await channel.DisconnectAsync();
        }
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
