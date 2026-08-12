using Microsoft.AspNetCore.Http;

namespace Halifax.Api.Middleware;

/// <summary>
/// Middleware that propagates or generates a correlation ID for request tracing.
/// </summary>
/// <remarks>
/// For each request it reads the <c>X-Correlation-Id</c> header (see <see cref="HeaderName"/>) or generates a
/// new GUID when the header is absent, assigns it to <see cref="HttpContext.TraceIdentifier"/> so it flows
/// into logs, and echoes it back on the response under the same header. Registered first in the pipeline by
/// <see cref="AppExtensions.UseHalifax"/>.
/// </remarks>
/// <param name="next">The next delegate in the request pipeline.</param>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>The HTTP header name used to read and return the correlation ID (<c>X-Correlation-Id</c>).</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>
    /// Processes the request: resolves the correlation ID, sets it on the trace identifier and the response
    /// header, then invokes the rest of the pipeline.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task that completes when the remainder of the pipeline has finished processing the request.</returns>
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
