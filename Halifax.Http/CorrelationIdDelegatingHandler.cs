using Microsoft.AspNetCore.Http;

namespace Halifax.Http;

/// <summary>
/// Delegating handler that forwards the correlation ID from the current HTTP context to outgoing requests.
/// </summary>
public class CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private const string HeaderName = "X-Correlation-Id";

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        if (context != null && !request.Headers.Contains(HeaderName))
        {
            request.Headers.TryAddWithoutValidation(HeaderName, context.TraceIdentifier);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
