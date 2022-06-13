using Destructurama;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

public static class L
{
    static L()
    {
        Log.Logger = new LoggerConfiguration()
            .Destructure.ToMaximumStringLength(1000)
            .Destructure.ToMaximumDepth(5)
            .Destructure.UsingAttributes()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Error)
            .Enrich.FromLogContext()
            .WriteTo.Console(theme: AnsiConsoleTheme.Literate)
            //.WriteTo.Console(new JsonFormatter())
            .CreateLogger();
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