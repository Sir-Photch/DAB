using CliWrap;
using System.Text;

namespace DAB.Util.Audio;

internal static class PCMAudioEncoder
{
    /// <exception cref="ArgumentException"></exception>
    internal static async Task<Stream> EncodeAsync(Stream toBeEncoded)
    {
        // TODO ensure ffmpeg is installed on system
        if (!toBeEncoded.CanRead) throw new ArgumentException("Not readable", nameof(toBeEncoded));

        if (toBeEncoded.CanSeek)
            toBeEncoded.Position = 0;

        MemoryStream stdOut = new();
        StringBuilder stdErr = new();

        Command ffmpeg = PipeSource.FromStream(toBeEncoded, true) |
                         Cli.Wrap("ffmpeg").WithArguments("-hide_banner -loglevel level+error -i - -ac 2 -f s16le -ar 48000 pipe:1")
                         | (PipeTarget.ToStream(stdOut, true), PipeTarget.ToStringBuilder(stdErr, Encoding.ASCII));

        var result = await ffmpeg.WithValidation(CommandResultValidation.None)
                                 .ExecuteAsync();

        if (result.ExitCode != 0 || stdErr.Length != 0)
            Log.Write(result.ExitCode != 0 ? ERR : DBG, "ffmpeg exited with code {exitCode} | stderr-dump: {dump}", result.ExitCode, stdErr.ToString());

        return stdOut;
    }
}
