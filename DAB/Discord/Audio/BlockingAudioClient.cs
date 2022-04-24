using Discord;
using Discord.Audio;

namespace DAB.Discord.Audio;

internal class BlockingAudioClient : IAudioClient
{
    #region private fields

    private readonly IAudioClient _underlyingClient;
    private readonly SemaphoreSlim _clientSemaphore = new(1);

    #endregion

    public BlockingAudioClient(IAudioClient toBeWrapped)
    {
#if DEBUG
        if (toBeWrapped is null) throw new ArgumentNullException(nameof(toBeWrapped));
#endif
        _underlyingClient = toBeWrapped;
    }

    public async ValueTask<bool> AcquireAsync()
    {
        return await _clientSemaphore.WaitAsync(0);
    }

    public void Release()
    {
        _clientSemaphore.Release();
    }

    #region IAudioClient

    public ConnectionState ConnectionState => _underlyingClient.ConnectionState;

    public int Latency => _underlyingClient.Latency;

    public int UdpLatency => _underlyingClient.UdpLatency;

    public event Func<Task> Connected
    {
        add
        {
            _underlyingClient.Connected += value;
        }
        remove
        {
            _underlyingClient.Connected -= value;
        }
    }

    public event Func<Exception, Task> Disconnected
    {
        add
        {
            _underlyingClient.Disconnected += value;
        }
        remove
        {
            _underlyingClient.Disconnected -= value;
        }
    }

    public event Func<int, int, Task> LatencyUpdated
    {
        add
        {
            _underlyingClient.LatencyUpdated += value;
        }
        remove
        {
            _underlyingClient.LatencyUpdated -= value;
        }
    }

    public event Func<int, int, Task> UdpLatencyUpdated
    {
        add
        {
            _underlyingClient.UdpLatencyUpdated += value;
        }
        remove
        {
            _underlyingClient.UdpLatencyUpdated -= value;
        }
    }

    public event Func<ulong, AudioInStream, Task> StreamCreated
    {
        add
        {
            _underlyingClient.StreamCreated += value;
        }
        remove
        {
            _underlyingClient.StreamCreated -= value;
        }
    }

    public event Func<ulong, Task> StreamDestroyed
    {
        add
        {
            _underlyingClient.StreamDestroyed += value;
        }
        remove
        {
            _underlyingClient.StreamDestroyed -= value;
        }
    }

    public event Func<ulong, bool, Task> SpeakingUpdated
    {
        add
        {
            _underlyingClient.SpeakingUpdated += value;
        }
        remove
        {
            _underlyingClient.SpeakingUpdated -= value;
        }
    }

    public AudioOutStream CreateDirectOpusStream()
    {
        return _underlyingClient.CreateDirectOpusStream();
    }

    public AudioOutStream CreateDirectPCMStream(AudioApplication application, int? bitrate = null, int packetLoss = 30)
    {
        return _underlyingClient.CreateDirectPCMStream(application, bitrate, packetLoss);
    }

    public AudioOutStream CreateOpusStream(int bufferMillis = 1000)
    {
        return _underlyingClient.CreateOpusStream(bufferMillis);
    }

    public AudioOutStream CreatePCMStream(AudioApplication application, int? bitrate = null, int bufferMillis = 1000, int packetLoss = 30)
    {
        return _underlyingClient.CreatePCMStream(application, bitrate, bufferMillis, packetLoss);
    }

    public void Dispose()
    {
        _underlyingClient.Dispose();
    }

    public IReadOnlyDictionary<ulong, AudioInStream> GetStreams()
    {
        return _underlyingClient.GetStreams();
    }

    public Task SetSpeakingAsync(bool value)
    {
        return _underlyingClient.SetSpeakingAsync(value);
    }

    public Task StopAsync()
    {
        return _underlyingClient.StopAsync();
    }

    #endregion
}
