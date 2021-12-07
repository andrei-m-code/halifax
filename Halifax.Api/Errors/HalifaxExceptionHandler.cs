using Halifax.Domain;
using Halifax.Domain.Exceptions;
using System.Net;

namespace Halifax.Api.Errors;

public class DefaultExceptionHandler : IExceptionHandler
{
    public Task<(object Response, HttpStatusCode Code)> HandleAsync(Exception exception)
    {
        var code = exception switch
        {
            HalifaxNotFoundException => HttpStatusCode.NotFound,
            HalifaxUnauthorizedException => HttpStatusCode.Unauthorized,
            HalifaxException => HttpStatusCode.BadRequest,
            _ => HttpStatusCode.InternalServerError
        };

        var result = (Response: (object) ApiResponse.With(exception), Code: code);

        return Task.FromResult(result);
    }
}
