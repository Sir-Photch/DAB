using FFMpegCore;
using FFMpegCore.Pipes;

namespace DAB.Discord.Audio;

internal static class AudioEncoder
{
    internal static async Task<Stream> EncodePCMAsync(Stream stream)
    {
#if DEBUG
        if (stream is null) throw new ArgumentNullException(nameof(stream));
#endif
        if (stream.CanSeek)
            stream.Seek(0, SeekOrigin.Begin);

        MemoryStream retval = new();

        await FFMpegArguments.FromPipeInput(new StreamPipeSource(stream))
                             .OutputToPipe(new StreamPipeSink(retval), addArguments: options => options
                                .WithCustomArgument("-f s16le -ac 2 -ar 48000"))
                             .ProcessAsynchronously(true);

        return retval;
    }
}
