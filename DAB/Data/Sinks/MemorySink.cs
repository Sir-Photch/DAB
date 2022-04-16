using System.Collections.Concurrent;
using DAB.Data.Interfaces;
using Microsoft.VisualStudio.Threading;

namespace DAB.Data.Sinks;

internal class MemorySink : IAnnouncementSink, IDisposable
{
    private readonly ConcurrentDictionary<ulong, MemoryStream> _userData = new();
    private bool _disposed = false;

    public int DataSizeCapBytes { get; init; } = 1_000_000; // 1 MB

    public async Task SaveAsync(ulong userId, Stream data)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(_userData));

        if (!data.CanRead)
            throw new ArgumentException("not readable", nameof(data));

        if (data.CanSeek)
            data.Position = 0;

        MemoryStream ms = new();
        try
        {
            int dataByte;
            while ((dataByte = data.ReadByte()) != -1)
            {
                ms.WriteByte((byte)dataByte);
            }
        }
        catch (Exception e)
        {

        }

        if (_userData.ContainsKey(userId))
           _userData[userId].DisposeAsync().Forget();

        _userData[userId] = ms;
    }

    public Task<Stream> LoadAsync(ulong userId)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(_userData));

        return Task.FromResult<Stream>(_userData[userId]);
    }

    public async Task ClearAsync(ulong userId)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(_userData));

        _userData.Remove(userId, out MemoryStream? ms);
        if (ms is not null)
            await ms.DisposeAsync();
    }

    public Task<bool> UserHasDataAsync(ulong userId)
    {
        return Task.FromResult(_userData.ContainsKey(userId));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach ((_, MemoryStream stream) in _userData)
            stream.Dispose();

        _userData.Clear();

        _disposed = true;
    }    
}
