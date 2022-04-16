using DAB.Discord.Commands;

namespace DAB.Discord.Enums;

#pragma warning disable IDE0055 // auto formatting
internal enum SlashCommandType
{
    INVALID,
    [SlashCommandDetails("set-chime", "set a chime given a file")]
    SET_CHIME,
    [SlashCommandDetails("clear-chime", "clear your chime")]
    CLEAR_CHIME
}
#pragma warning restore IDE0055
