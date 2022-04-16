using DAB.Data.Interfaces;
using System.Runtime.CompilerServices;

namespace DAB.Data.Sinks
{
    internal class FileSystemSink : IUserDataSink
    {
        private readonly DirectoryInfo _directory;

        public FileSystemSink(string folderPath)
        {
            _directory = Directory.CreateDirectory(folderPath);
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

        public async Task SaveAsync(ulong userId, Stream data)
        {
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
}
