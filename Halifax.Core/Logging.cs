using Destructurama;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Serilog.ILogger;

/// <summary>
/// Serilog-based logging facade with pre-configured console output.
/// </summary>
// ReSharper disable once CheckNamespace
public static class L
{
    private static Lazy<ILogger> loggerLazy = new(() => configuration!.CreateLogger());

    private static LoggerConfiguration configuration = new LoggerConfiguration()
        .MinimumLevel.Debug()
        //.Destructure.ToMaximumStringLength(1000)
        .Destructure.ToMaximumDepth(5)
        .Destructure.UsingAttributes()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Error)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:M/d, HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            theme: AnsiConsoleTheme.Literate);

    private static ILogger Log => loggerLazy.Value;

    /// <summary>
    /// Configures the logger by modifying the existing <see cref="LoggerConfiguration"/>.
    /// </summary>
    /// <param name="configure">Action to modify the logger configuration.</param>
    public static void Configure(Action<LoggerConfiguration> configure)
    {
        configure(configuration);
        if (loggerLazy.IsValueCreated)
        {
            loggerLazy = new(() => configuration.CreateLogger());
        }
    }
    
    /// <summary>
    /// Replaces the logger configuration entirely.
    /// </summary>
    /// <param name="loggerConfiguration">The new logger configuration.</param>
    public static void Configure(LoggerConfiguration loggerConfiguration)
    {
        configuration = loggerConfiguration;
        if (loggerLazy.IsValueCreated)
        {
            loggerLazy = new(() => configuration.CreateLogger());
        }
    }
    
    /// <summary>Logs an information message with a single property value.</summary>
    public static void Info<TPropertyValue>(string messageTemplate, TPropertyValue propertyValue) =>
        Log.Information(messageTemplate, propertyValue);
    
    /// <summary>Logs an information message.</summary>
    public static void Info(string messageTemplate, params object[] propertyValues) =>
        Log.Information(messageTemplate, propertyValues);

    /// <summary>Logs a warning message.</summary>
    public static void Warning(string messageTemplate, params object[] propertyValues) =>
        Log.Warning(messageTemplate, propertyValues);
    
    /// <summary>Logs a warning message with an exception.</summary>
    public static void Warning(Exception exception, string messageTemplate, params object[] propertyValues) =>
        Log.Warning(exception, messageTemplate, propertyValues);
    
    /// <summary>Logs an error message.</summary>
    public static void Error(string messageTemplate, params object[] propertyValues) =>
        Log.Error(messageTemplate, propertyValues);
    
    /// <summary>Logs an error message with an exception.</summary>
    public static void Error(Exception exception, string messageTemplate, params object[] propertyValues) =>
        Log.Error(exception, messageTemplate, propertyValues);

    /// <summary>Logs a fatal error message.</summary>
    public static void Fatal(string messageTemplate, params object[] propertyValues) =>
        Log.Error(messageTemplate, propertyValues);
    
    /// <summary>Logs a fatal error message with an exception.</summary>
    public static void Fatal(Exception exception, string messageTemplate, params object[] propertyValues) =>
        Log.Error(exception, messageTemplate, propertyValues);
}

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> logging configuration.
/// </summary>
public static class ServicesExtensions
{
    /// <summary>
    /// Cleanup default Microsoft and System logging
    /// </summary>
    public static void CleanupDefaultLogging(this IServiceCollection services)
    {
        services.AddLogging(logging => logging
            .AddFilter("Microsoft", LogLevel.Critical)
            .AddFilter("System", LogLevel.Critical));
    }
}

/// <summary>
/// Marks a property to be excluded from structured log output.
/// </summary>
public class NotLoggedAttribute : Destructurama.Attributed.NotLoggedAttribute {}