using Discord;
using Discord.Audio;

namespace DAB.Discord;

/*
 * https://gist.github.com/Joe4evr/773d3ce6cc10dbea6924d59bbfa3c62a
 */
internal class AudioClientManager : IDisposable, IAsyncDisposable
{
    private readonly Dictionary<ulong, IAudioClient> _activeAudioClients = new();
    private readonly SemaphoreSlim _dictSemaphore = new(1);

    private static AudioClientManager? _instance;

    internal static AudioClientManager Instance => _instance ??= new();

    private AudioClientManager() { }

    internal async Task<IAudioClient> GetClientAsync(IVoiceChannel target, CancellationToken token = default)
    {
        await _dictSemaphore.WaitAsync(token);

        try
        {
            if (_activeAudioClients.ContainsKey(target.Id))
            {
                IAudioClient knownClient = _activeAudioClients[target.Id];
                if (knownClient.ConnectionState is ConnectionState.Connected or ConnectionState.Connecting)
                    return knownClient;                
            }

            
            return _activeAudioClients[target.Id] = await target.ConnectAsync();
        }
        catch (Exception e)
        {
            Log.Write(FTL, e, "GetClientAsync error!");
            throw;
        }
        finally
        {
            _dictSemaphore.Release();
        }
    }

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
}
