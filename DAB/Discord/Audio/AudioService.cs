using System.Collections.Concurrent;
using Discord;
using Discord.Audio;

namespace DAB.Discord.Audio;

/*
 * https://gist.github.com/Joe4evr/773d3ce6cc10dbea6924d59bbfa3c62a
 */
public class AudioService
{
    private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedChannels = new();

    public async Task JoinAudioAsync(IGuild guild, IVoiceChannel target)
    {
        if (guild is null)
            throw new ArgumentNullException(nameof(guild));

        if (target is null)
            throw new ArgumentNullException(nameof(target));

        if (target.Guild.Id != guild.Id)
            return;

        if (_connectedChannels.ContainsKey(guild.Id))
            return;

        IAudioClient client = await target.ConnectAsync();

        if (_connectedChannels.TryAdd(guild.Id, client))
            Log.Write(INF, "Connected to voice on {guild}", guild.Name);
    }

    public async Task LeaveAudio(IGuild guild)
    {
        if (_connectedChannels.TryRemove(guild.Id, out IAudioClient? client) && client is not null)
        {
            await client.StopAsync();
            Log.Write(INF, "Disconnected from void on {guild}", guild.Name);
        }
    }

    public async Task SendAudioAsync(IGuild guild, Stream stream)
    {
        if (!stream.CanRead)
            throw new ArgumentException("not readable", nameof(stream));

        if (_connectedChannels.TryGetValue(guild.Id, out IAudioClient? client) && client is not null)
        {
            await using AudioOutStream opus = client.CreatePCMStream(AudioApplication.Music);
            try
            {
                await stream.CopyToAsync(opus);
            }
            finally
            {
                await opus.FlushAsync();
            }
        }
    }
}
