using DAB.Data.Interfaces;
using System.Runtime.CompilerServices;

namespace DAB.Data.Sinks;

internal class FileSystemSink : IUserDataSink
{
    private readonly DirectoryInfo _directory;

    public FileSystemSink(string folderPath)
    {
#if DEBUG
        if (folderPath is null) throw new ArgumentNullException(nameof(folderPath));
#endif
        try
        {
            _directory = Directory.CreateDirectory(folderPath);
        }
        catch (Exception e)
        {
            Log.Write(FTL, e, "Invalid {argument} for {instance}", folderPath, nameof(FileSystemSink));
            throw;
        }
    }

    public int DataSizeCapBytes { get; init; } = 5_000_000;

    public Task ClearAsync(ulong userId)
    {
        File.Delete(GetUserFileName(userId));

        return Task.CompletedTask;
    }

    public async Task<Stream?> LoadAsync(ulong userId)
    {
        if (!await UserHasDataAsync(userId))
            return null;

        MemoryStream ms = new();

        using FileStream fs = File.OpenRead(GetUserFileName(userId));
        await fs.CopyToAsync(ms);

        return ms;
    }

    /// <exception cref="ArgumentException"></exception>
    public async Task SaveAsync(ulong userId, Stream data)
    {
#if DEBUG
        if (data is null) throw new ArgumentNullException(nameof(data));
#endif
        if (!data.CanRead)
            throw new ArgumentException("not readable", nameof(data));

        if (data.CanSeek)
            data.Position = 0;

        using FileStream fs = File.Create(GetUserFileName(userId));

        await data.CopyToAsync(fs);
    }

    public Task<bool> UserHasDataAsync(ulong userId)
    {
        bool fileExists = File.Exists(GetUserFileName(userId));

        return Task.FromResult(fileExists);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetUserFileName(ulong userId)
        => Path.Combine(_directory.FullName, userId.ToString());
}
