using Discord;
using Discord.Audio;

namespace DAB.Discord.Audio;

internal class AudioClientManager : IDisposable, IAsyncDisposable
{
    #region private fields
    private readonly Dictionary<ulong, BlockingAudioClient> _activeAudioClients = new();
    private readonly SemaphoreSlim _dictSemaphore = new(1);

    private static AudioClientManager? _instance;
    #endregion

    internal static AudioClientManager Instance => _instance ??= new();

    #region ctor
    private AudioClientManager() { }
    #endregion

    #region methods

    internal async Task<BlockingAudioClient> GetClientAsync(IVoiceChannel target, bool selfDeaf = true, bool selfMute = false, bool external = false, CancellationToken token = default)
    {
#if DEBUG
        if (target is null) throw new ArgumentNullException(nameof(target));
#endif

        await _dictSemaphore.WaitAsync(token);

        try
        {
            if (_activeAudioClients.ContainsKey(target.Id))
            {
                BlockingAudioClient knownClient = _activeAudioClients[target.Id];
                if (knownClient.ConnectionState is ConnectionState.Connected or ConnectionState.Connecting)
                    return knownClient;
            }

            return _activeAudioClients[target.Id] = new(await target.ConnectAsync(selfDeaf: selfDeaf, selfMute: selfMute, external: external));
        }
        catch (Exception e)
        {
            Log.Write(FTL, e, "Could not connect audio-client!");
            throw;
        }
        finally
        {
            _dictSemaphore.Release();
        }
    }

    #region IDisposable

    public void Dispose()
    {
        foreach ((_, IAudioClient client) in _activeAudioClients)
            client.Dispose();

        _activeAudioClients.Clear();

        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await Task.WhenAll(_activeAudioClients.Values.Select(value => value.StopAsync()));

        Dispose();
    }

    #endregion

    #endregion
}
