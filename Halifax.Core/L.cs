using System;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

public static class L
{
    static L()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Error)
            .Enrich.FromLogContext()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            .CreateLogger();
    }

    public static void Info(string info) => Log.Information(info);

    public static void Error(string message, Exception exception = null) => Log.Error(exception, message);

    public static void Warning(string message, Exception exception = null) => Log.Warning(exception, message);

    public static void Fatal(string message, Exception exception) => Log.Fatal(exception, message);
}