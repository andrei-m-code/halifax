using Halifax.Domain;
using Halifax.Domain.Exceptions;
using System.Net;
using Halifax.Api.Extensions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Halifax.Api.Errors;

/// <summary>
/// Global exception handler that maps Halifax exceptions to HTTP status codes and logs error details.
/// </summary>
public class HalifaxExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// Logs the error request details. Override to customize error logging behavior.
    /// </summary>
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