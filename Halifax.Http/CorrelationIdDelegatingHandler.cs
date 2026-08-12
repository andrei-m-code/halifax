using Microsoft.AspNetCore.Http;

namespace Halifax.Http;

/// <summary>
/// A <see cref="DelegatingHandler"/> that forwards the current request's correlation ID to outgoing HTTP
/// requests by adding an <c>X-Correlation-Id</c> header.
/// </summary>
/// <remarks>
/// The correlation ID is taken from the ambient <see cref="HttpContext.TraceIdentifier"/> exposed by the
/// injected <see cref="IHttpContextAccessor"/>. The header is only added when an HTTP context is available
/// and the outgoing request does not already carry the header, so an explicitly set value is preserved.
/// Attach this handler to a client via its <c>IHttpClientBuilder</c>, for example with
/// <c>AddHttpMessageHandler&lt;CorrelationIdDelegatingHandler&gt;()</c>.
/// </remarks>
/// <param name="httpContextAccessor">Accessor used to read the current <see cref="HttpContext"/>.</param>
public class CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private const string HeaderName = "X-Correlation-Id";

    /// <summary>
    /// Adds the correlation ID header from the current HTTP context (when present and not already set) and
    /// forwards the request to the inner handler.
    /// </summary>
    /// <param name="request">The outgoing HTTP request message.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The HTTP response produced by the inner handler.</returns>
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
