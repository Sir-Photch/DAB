using Microsoft.VisualStudio.Threading;
using System.Diagnostics;

namespace DAB.Audio;

internal static class PCMAudioPlayer
{
    internal static Stream Create(string filePath, int bitrate = 48000)
    {
        var ffmpeg = Process.Start(new ProcessStartInfo()
        {
            FileName = "ffmpeg.exe",
            Arguments = $"-hide_banner -loglevel panic -i \"{filePath}\" -ac 2 -f s16le -ar 48000 pipe:1",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (ffmpeg is null)
            throw new ApplicationException("Could not start ffmpeg.exe");

        Task.Run(async () =>
        {
            string stderr = await ffmpeg.StandardError.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(stderr))
                return;

            Log.Write(DBG, "ffmpeg stderr for file {filePath} was {stderr}", filePath, stderr);
        }).Forget();

        return ffmpeg.StandardOutput.BaseStream;
    }
}
