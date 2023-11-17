using Halifax.Domain;
using Halifax.Domain.Exceptions;
using System.Net;
using Halifax.Api.Extensions;
using Microsoft.AspNetCore.Http;

namespace Halifax.Api.Errors;

public class HalifaxExceptionHandler : IHalifaxExceptionHandler
{
    public virtual async Task<(object Response, HttpStatusCode Code)> HandleAsync(
        HttpContext context,
        Exception exception)
    {
        var code = exception switch
        {
            HalifaxNotFoundException => HttpStatusCode.NotFound,
            HalifaxUnauthorizedException => HttpStatusCode.Unauthorized,
            HalifaxException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        await LogErrorRequestAsync(context, exception);

        return
        (
            Response: ApiResponse.With(exception),
            Code: code
        );
    }

    protected virtual async Task LogErrorRequestAsync(HttpContext context, Exception exception)
    {
        var requestString = await context.Request.GetRequestStringAsync();
        L.Error(exception, exception.Message);
        L.Error(requestString);
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }

        await LogErrorRequestAsync(httpContext, exception);
        
        return true;
    }
}