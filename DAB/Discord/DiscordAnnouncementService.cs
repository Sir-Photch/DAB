using DAB.Configuration;
using DAB.Data.Interfaces;
using DAB.Data.Sinks;
using DAB.Discord.Audio;
using DAB.Discord.Commands;
using DAB.Discord.Enums;
using Discord;
using Discord.WebSocket;
using Microsoft.VisualStudio.Threading;
using System.Collections.Concurrent;

namespace DAB.Discord;

internal class DiscordAnnouncementService
{
    #region fields

    private readonly DiscordSocketClient _socketClient;
    private readonly IUserDataSink _announcementSink;
    private readonly AudioClientManager _audioClientManager;

    private readonly int _sendAudioTimeoutMs;
    private readonly int _bufferMs;

    private readonly ConcurrentDictionary<ulong, ConcurrentQueue<Stream>> _sendAudioQueue = new();

    #endregion

    #region ctor

    internal DiscordAnnouncementService()
    {
        _audioClientManager = GlobalServiceProvider.GetService<AudioClientManager>();
        _socketClient = GlobalServiceProvider.GetService<DiscordSocketClient>();
        _announcementSink = GlobalServiceProvider.GetService<IUserDataSink>();

#if DEBUG
        if (_audioClientManager is null || _socketClient is null) throw new NotSupportedException($"Missing services in {nameof(GlobalServiceProvider)}");
#endif
        if (_announcementSink is null)
        {
            Log.Write(WRN, "No datasink configured! Will default to volatile memory-sink");
            _announcementSink = new MemorySink();
        }

        ConfigRoot? root = GlobalServiceProvider.GetService<ConfigRoot>();

        _sendAudioTimeoutMs = root?.Bot.ChimePlaybackTimeoutMs ?? 10_000;
        _bufferMs = root?.Discord.AudioBufferMs ?? 25;

        _socketClient.Ready += OnClientReadyAsync;

        _socketClient.UserVoiceStateUpdated += OnClientUserVoiceStateUpdatedAsync;
        _socketClient.SlashCommandExecuted += OnSlashCommandExecutedAsync;
    }

    #endregion

    #region event-subscribers

    private async Task OnSlashCommandExecutedAsync(SocketSlashCommand command)
    {
        SlashCommandType? cmdType = SlashCommandFactory.FromString(command.Data.Name);

        if (cmdType is null)
            return;

        try
        {
            await SlashCommandFactory.HandleCommandAsync(new SlashCommand
            {
                Command = command,
                CommandType = cmdType.Value
            });
        }
        catch (Exception e)
        {
            Log.Write(ERR, e, "Error in {method}", nameof(SlashCommandFactory.HandleCommandAsync));
        }
    }

    private async Task OnClientReadyAsync()
    {
        try
        {
            // TODO refactor this, to only apply commands associated with announcement.
            // for now, the only thing this bot is doing is announcing, so there is no need (yet) xP
            await _socketClient.OverwriteCommandsAsync();
        }
        catch (Exception e)
        {
            Log.Write(ERR, e, "Could not create slash-commands!");
        }
    }

    private async Task OnClientUserVoiceStateUpdatedAsync(
        SocketUser user, SocketVoiceState prev, SocketVoiceState curr)
    {
        if (user.IsBot || user.IsWebhook)
            return;

        // disconnected or not visible to bot
        if (curr.VoiceChannel is null)
            return;

        // switching channels within guild
        if (prev.VoiceChannel is not null &&
            prev.VoiceChannel.Guild.Id == curr.VoiceChannel.Guild.Id)
            return;

        if (!await _announcementSink.UserHasDataAsync(user.Id))
            return;

        StartAudioAsync(curr.VoiceChannel.Id, user.Id).Forget();
    }

    #endregion

    #region methods

    private async Task StartAudioAsync(ulong channelId, ulong userId)
    {
        if (await _announcementSink.LoadAsync(userId) is not Stream userData)
            return;

        if (await _socketClient.GetChannelAsync(channelId) is not IVoiceChannel channel)
            return;

        (_sendAudioQueue.ContainsKey(channelId)
            ? _sendAudioQueue[channelId]
            : (_sendAudioQueue[channelId] = new())).Enqueue(userData);

        await Task.Delay(500); // HACK

        BlockingAudioClient audioClient;
        try
        {
            audioClient = await _audioClientManager.GetClientAsync(channel, selfDeaf: true);
        }
        catch (Exception ex)
        {
            Log.Write(FTL, ex, "Could not get audio-client!");
            return;
        }

        // bail if client is already playing on channel
        if (!await audioClient.AcquireAsync())
            return;

        try
        {
            if (!_sendAudioQueue.TryGetValue(channel.Id, out ConcurrentQueue<Stream>? audioQueue) ||
                audioQueue is null)
                return;

            while (audioQueue.TryDequeue(out Stream? stream) && stream is not null)
            {
                stream.Position = 0;
                CancellationTokenSource cts = new(_sendAudioTimeoutMs);
                try
                {
                    await audioClient.SendPCMEncodedAudioAsync(stream: stream,
                                                               bufferMillis: _bufferMs,
                                                               token: cts.Token);
                }
                catch (OperationCanceledException)
                {
                    Log.Write(DBG, "Playing audio ran into timeout of {field}: {ms}", nameof(_sendAudioTimeoutMs), _sendAudioTimeoutMs);
                }
            }

            if (_sendAudioQueue.TryRemove(channel.Id, out ConcurrentQueue<Stream>? removedQueue))
                removedQueue?.Clear();
        }
        catch (Exception e)
        {
            Log.Write(ERR, e, "Unexpected error in {method}", nameof(StartAudioAsync));
        }
        finally
        {
            audioClient.Release();
            await audioClient.StopAsync();
            await channel.DisconnectAsync();
        }
    }

    #endregion
}
