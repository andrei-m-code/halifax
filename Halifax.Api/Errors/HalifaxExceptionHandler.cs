using Halifax.Domain;
using Halifax.Domain.Exceptions;
using System.Net;
using Halifax.Api.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Halifax.Api.Errors;

/// <summary>
/// Global <see cref="IExceptionHandler"/> that maps unhandled Halifax exceptions to HTTP status codes,
/// logs the failing request, and writes a standardized <see cref="ApiResponse"/> error body.
/// </summary>
/// <remarks>
/// Registered by default during <see cref="AppExtensions.AddHalifax"/> unless disabled via
/// <see cref="App.HalifaxBuilder.ConfigureExceptionHandler"/>. Exceptions map as follows:
/// <see cref="HalifaxNotFoundException"/> to 404, <see cref="HalifaxUnauthorizedException"/> to 401, any other
/// <see cref="HalifaxException"/> to 400, and everything else to 500.
/// </remarks>
public class HalifaxExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// Logs the failing request (its formatted details) together with the exception. Override to customize
    /// error logging behavior.
    /// </summary>
    /// <param name="context">The HTTP context of the request that failed.</param>
    /// <param name="exception">The exception that was thrown while processing the request.</param>
    /// <returns>A task that completes once the request and exception have been logged.</returns>
    protected virtual async Task LogErrorRequestAsync(HttpContext context, Exception exception)
    {
        var requestString = await context.Request.GetRequestStringAsync();
        L.Error(exception, exception.Message);
        L.Error(requestString);
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        
        var code = exception switch
        {
            HalifaxNotFoundException => HttpStatusCode.NotFound,
            HalifaxUnauthorizedException => HttpStatusCode.Unauthorized,
            HalifaxException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        await LogErrorRequestAsync(context, exception);
        
        context.Response.StatusCode = (int) code;
        await context.Response.WriteAsJsonAsync(ApiResponse.With(exception), cancellationToken: cancellationToken);
        
        // Return false to continue with the default behavior
        // - or - return true to signal that this exception is handled
        return true;
    }
}