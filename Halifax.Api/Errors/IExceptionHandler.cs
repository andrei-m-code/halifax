using System.Net;

namespace Halifax.Api.Errors;

public interface IExceptionHandler
{
    Task<(object Response, HttpStatusCode Code)> HandleAsync(Exception exception);
}
