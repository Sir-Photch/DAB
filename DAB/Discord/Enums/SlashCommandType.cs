using DAB.Discord.Commands;

namespace DAB.Discord.Enums;

internal enum SlashCommandType
{
    INVALID,
    [SlashCommandDetails("set-chime", "set a chime given a file")]
    SET_CHIME,
    [SlashCommandDetails("clear-chime", "clear your chime")]
    CLEAR_CHIME
}
