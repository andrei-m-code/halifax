using Destructurama;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Serilog.ILogger;

public static class L
{
    private static Lazy<ILogger> loggerLazy = new(() => configuration.CreateLogger());

    private static LoggerConfiguration configuration = new LoggerConfiguration()
        //.Destructure.ToMaximumStringLength(1000)
        .Destructure.ToMaximumDepth(5)
        .Destructure.UsingAttributes()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Error)
        .Enrich.FromLogContext()
        .WriteTo.Console(theme: AnsiConsoleTheme.Literate);

    private static ILogger Log => loggerLazy.Value;

    public static void Configure(Action<LoggerConfiguration> configure)
    {
        configure(configuration);
        if (loggerLazy.IsValueCreated)
        {
            loggerLazy = new(() => configuration.CreateLogger());
        }
    }
    
    public static void Configure(LoggerConfiguration loggerConfiguration)
    {
        configuration = loggerConfiguration;
        if (loggerLazy.IsValueCreated)
        {
            loggerLazy = new(() => configuration.CreateLogger());
        }
    }
    
    public static void Info<TPropertyValue>(string messageTemplate, TPropertyValue propertyValue) =>
        Log.Information(messageTemplate, propertyValue);
    
    public static void Info(string messageTemplate, params object[] propertyValues) => 
        Log.Information(messageTemplate, propertyValues);

    public static void Warning(string messageTemplate, params object[] propertyValues) =>
        Log.Warning(messageTemplate, propertyValues);
    
    public static void Warning(Exception exception, string messageTemplate, params object[] propertyValues) =>
        Log.Warning(exception, messageTemplate, propertyValues);
    
    public static void Error(string messageTemplate, params object[] propertyValues) =>
        Log.Error(messageTemplate, propertyValues);
    
    public static void Error(Exception exception, string messageTemplate, params object[] propertyValues) =>
        Log.Error(exception, messageTemplate, propertyValues);

    public static void Fatal(string messageTemplate, params object[] propertyValues) =>
        Log.Error(messageTemplate, propertyValues);
    
    public static void Fatal(Exception exception, string messageTemplate, params object[] propertyValues) =>
        Log.Error(exception, messageTemplate, propertyValues);
    
    [Obsolete]
    public static void Error(string message, Exception exception = null) => Log.Error(exception, message);

    [Obsolete]
    public static void Warning(string message, Exception exception = null) => Log.Warning(exception, message);

    [Obsolete]
    public static void Fatal(string message, Exception exception) => Log.Fatal(exception, message);
}

public static class ServicesExtensions
{
    /// <summary>
    /// Cleanup default Microsoft and System logging
    /// </summary>
    public static void CleanupDefaultLogging(this IServiceCollection services)
    {
        services.AddLogging(logging => logging
            .AddFilter("Microsoft", LogLevel.Error)
            .AddFilter("System", LogLevel.Error));
    }
}

public class NotLoggedAttribute : Destructurama.Attributed.NotLoggedAttribute {}