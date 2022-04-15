using Discord;

namespace DAB.Util;

internal static class Extensions
{
    internal static void Deconstruct(this LogMessage msg, out Log.Level level, out string source, out string message, out Exception? exception)
    {
        level = msg.Severity switch
        {
            LogSeverity.Critical => FTL,
            LogSeverity.Error => ERR,
            LogSeverity.Warning => WRN,
            LogSeverity.Info => INF,
            LogSeverity.Verbose => VRB,
            LogSeverity.Debug => DBG
        };
        source = msg.Source;
        message = msg.Message;
        exception = msg.Exception;
    }
}
