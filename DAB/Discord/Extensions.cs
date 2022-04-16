using Discord.Audio;

namespace DAB.Discord;

internal static class Extensions
{
    internal static async Task SendPCMEncodedAudioAsync(this IAudioClient client, Stream stream, CancellationToken token = default)
    {
        if (!stream.CanRead)
            throw new ArgumentException("not readable", nameof(stream));

        await using AudioOutStream pcmstream = client.CreatePCMStream(AudioApplication.Music);

        try { await stream.CopyToAsync(pcmstream, token); }
        finally { await pcmstream.FlushAsync(token); }
    }
}
