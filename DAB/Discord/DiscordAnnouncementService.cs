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
using System.Reflection;

namespace DAB.Discord;

internal class DiscordAnnouncementService
{
    #region fields

    private readonly IServiceProvider _serviceProvider;

    private readonly DiscordSocketClient _socketClient;

    private readonly IUserDataSink _announcementSink;
    private readonly AudioClientManager _audioClientManager;

    private readonly int _sendAudioTimeoutMs;

    private readonly ConcurrentDictionary<ulong, ConcurrentQueue<Stream>> _sendAudioQueue = new();

    #endregion

    #region ctor

    internal DiscordAnnouncementService(IServiceProvider serviceProvider)
    {
#if DEBUG
        if (serviceProvider is null) throw new ArgumentNullException(nameof(serviceProvider));
#endif

        _serviceProvider = serviceProvider;

#if DEBUG
#pragma warning disable CS8601 // method will throw in debug mode when fields are assigned to null
#endif
        _audioClientManager = _serviceProvider.GetService(typeof(AudioClientManager)) as AudioClientManager;
        _socketClient = _serviceProvider.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient;
        _announcementSink = _serviceProvider.GetService(typeof(IUserDataSink)) as IUserDataSink;        
#if DEBUG
#pragma warning restore CS8601
#endif

#if DEBUG
        if (_audioClientManager is null || _socketClient is null) throw new NotSupportedException($"Missing services in {nameof(serviceProvider)}");
#endif
        if (_announcementSink is null)
        {
            Log.Write(WRN, "No datasink configured! Will default to volatile memory-sink");
            _announcementSink = new MemorySink();
        }

        _sendAudioTimeoutMs = (_serviceProvider.GetService(typeof(ConfigRoot)) as ConfigRoot)
                            ?.Bot.ChimePlaybackTimeoutMs ?? 10_000;

        _socketClient.Ready += OnClientReadyAsync;

        _socketClient.UserVoiceStateUpdated += OnClientUserVoiceStateUpdatedAsync;
        _socketClient.SlashCommandExecuted += OnSlashCommandExecutedAsync;

        SlashCommandFactory.Initlaize(_serviceProvider);
    }

    #endregion

    #region event-subscribers

    private async Task OnSlashCommandExecutedAsync(SocketSlashCommand command)
    {
        var possibleCommandNames = typeof(SlashCommandType).GetFields()
                                                       .Select(f => f.GetCustomAttribute<SlashCommandDetailsAttribute>())
                                                       .Select(attr => attr?.CommandName)
                                                       .Where(name => name == command.Data.Name);

        if (!possibleCommandNames.Any())
            return;

        SlashCommandType? cmdType = SlashCommandFactory.FromString(possibleCommandNames.First());

        if (cmdType is null)
            return;

        try
        {
            await command.DeferAsync(ephemeral: true);
            IUserMessage response = await command.FollowupAsync("Thinking...", ephemeral: true);
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
            // for now, the only thing this bot is doing is announcing, so there is no need xP
            await _socketClient.ApplyAllCommandsAsync();
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

        BlockingAudioClient audioClient = await _audioClientManager.GetClientAsync(channel);

        // bail if client is already playing on channel
        if (!audioClient.Acquire())
            return;

        try
        {
            if (!_sendAudioQueue.TryGetValue(channel.Id, out ConcurrentQueue<Stream>? audioQueue) ||
                audioQueue is null)
                return;

            while (audioQueue.TryDequeue(out Stream? stream) && stream is not null)
            {
                stream.Position = 0;
                CancellationTokenSource cts = new(_sendAudioTimeoutMs); // HACK
                await audioClient.SendPCMEncodedAudioAsync(stream, cts.Token);
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
