using Discord;
using Discord.Commands;
using DAB.Audio;

namespace DAB.Discord.Audio;

public class AudioModule : ModuleBase<ICommandContext>
{
    private readonly AudioService _serivce;

    public AudioModule(AudioService service)
    {
        _serivce = service;
    }

    [Command("leave")]
    public async Task LeaveCmdAsync()
    {
        await _serivce.LeaveAudio(Context.Guild);
    }

    [Command("play", RunMode = RunMode.Async)]
    public async Task PlayCmdAsync([Remainder][Summary("fully qualified path to file")] string song)
    {
        try
        {
            await _serivce.JoinAudioAsync(Context.Guild, (Context.User as IVoiceState)?.VoiceChannel);
        }
        catch (ArgumentNullException ane)
        {
            Log.Write(DBG, ane, "Bad parameters on JoinAudioAsync {guild} {user}", Context.Guild, Context.User);
            return;
        }
        try
        {
            using Stream pcmStream = PCMAudioPlayer.Create(song);
            await _serivce.SendAudioAsync(Context.Guild, Context.Channel, pcmStream);
        }
        catch (Exception e)
        {
            Log.Write(FTL, e, "unexpected exception in playcmd");
        }
        await _serivce.LeaveAudio(Context.Guild);
    }
}
