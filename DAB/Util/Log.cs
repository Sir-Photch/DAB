using System.Text;
using System.Globalization;
using System.Runtime.CompilerServices;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Json;

namespace DAB.Util;

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
                                                                LogEventLevel.Warning,
                                                                fileSizeLimitBytes: null,
                                                                shared: true,
                                                                flushToDiskInterval: TimeSpan.FromMinutes(1),
                                                                encoding: Encoding.ASCII,
                                                                rollingInterval: RollingInterval.Day))
                                           .CreateLogger();
    }

    internal static void Flush() => Serilog.Log.CloseAndFlush();

    internal static void Write(Level level, Exception e, string template, params object[] args)
    {
        switch (level)
        {
            case Level.FTL: Serilog.Log.Fatal(e, template, args); break;
            case Level.ERR: Serilog.Log.Error(e, template, args); break;
            case Level.WRN: Serilog.Log.Warning(e, template, args); break;
#if DEBUG
            case Level.INF: Serilog.Log.Information(e, template, args); break;
            case Level.DBG: Serilog.Log.Debug(e, template, args); break;
            case Level.VRB: Serilog.Log.Verbose(e, template, args); break;
#endif
            default: break;
        }
    }

    internal static void Write(Level level, string template, params object[] args)
    {
        switch (level)
        {
            case Level.FTL: Serilog.Log.Fatal(template, args); break;
            case Level.ERR: Serilog.Log.Error(template, args); break;
            case Level.WRN: Serilog.Log.Warning(template, args); break;
#if DEBUG
            case Level.INF: Serilog.Log.Information(template, args); break;
            case Level.DBG: Serilog.Log.Debug(template, args); break;
            case Level.VRB: Serilog.Log.Verbose(template, args); break;
#endif
            default: break;
        }
    }


}
