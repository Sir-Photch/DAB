using CliWrap;

namespace DAB.Util.Audio;

internal static class PCMAudioEncoder
{
    internal static async Task<Stream> EncodeAsync(Stream toBeEncoded)
    {
        if (!toBeEncoded.CanRead)
            throw new ArgumentException("Not readable", nameof(toBeEncoded));

        if (toBeEncoded.CanSeek)
            toBeEncoded.Position = 0;

        MemoryStream output = new();

        Command ffmpeg = toBeEncoded | Cli.Wrap("ffmpeg").WithArguments("-hide_banner -loglevel panic -i - -ac 2 -f s16le -ar 48000 pipe:1") | output;

        if (ffmpeg is null)
            throw new ApplicationException("Could not start ffmpeg.exe");

        try
        {
            await ffmpeg.ExecuteAsync();
        }
        catch (Exception e)
        {

        }

        return output;
    }
}
