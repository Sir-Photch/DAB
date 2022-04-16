using System.Text;
using System.Globalization;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Json;
using System.Runtime.CompilerServices;

namespace DAB.Util
{
    internal static class Log
    {
        private static readonly DirectoryInfo _logDir;

        private const string LOGDIR_NAME = "logs";

        internal enum Level { VRB, DBG, INF, WRN, ERR, FTL }

        static Log()
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOGDIR_NAME);
            _logDir = Directory.CreateDirectory(logPath);

            Serilog.Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose()
                                               .Enrich.WithExceptionDetails()
#if DEBUG
                                               .WriteTo.Console()
#else
                                               .WriteTo.Async(a => a.Console())
#endif
                                               .WriteTo.Async(
                                                        a => a.File(new JsonFormatter(renderMessage: true, formatProvider: CultureInfo.InvariantCulture),
                                                                    _logDir.FullName + "/.log",
                                                                    LogEventLevel.Information,
                                                                    fileSizeLimitBytes: null,
                                                                    shared: true,
                                                                    flushToDiskInterval: TimeSpan.FromMinutes(1),
                                                                    encoding: Encoding.ASCII,
                                                                    rollingInterval: RollingInterval.Day))
                                               .CreateLogger();
        }

        internal static void Flush() => Serilog.Log.CloseAndFlush();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Write(Level level, Exception e, string template, params object[] args)
            => Serilog.Log.Write(ToSerilogLevel(level), e, template, args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Write(Level level, string template, params object[] args)
            => Serilog.Log.Write(ToSerilogLevel(level), template, args);

        private static LogEventLevel ToSerilogLevel(Level level) => level switch
        {
            VRB => LogEventLevel.Verbose,
            DBG => LogEventLevel.Debug,
            INF => LogEventLevel.Information,
            WRN => LogEventLevel.Warning,
            ERR => LogEventLevel.Error,
            FTL => LogEventLevel.Fatal
        };
    }
}
