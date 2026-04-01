using Microsoft.AspNetCore.Http;

namespace Halifax.Api.Middleware;

/// <summary>
/// Middleware that propagates or generates a correlation ID for request tracing.
/// Reads from the X-Correlation-Id header, or generates a new GUID if not present.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>The HTTP header name used for correlation IDs.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>Processes the request, setting the correlation ID on both the trace identifier and response header.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        await next(context);
    }
}
