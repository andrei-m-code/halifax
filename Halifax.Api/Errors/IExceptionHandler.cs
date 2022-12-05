using System.Net;
using Microsoft.AspNetCore.Http;

namespace Halifax.Api.Errors;

public interface IExceptionHandler
{
    Task<(object Response, HttpStatusCode Code)> HandleAsync(HttpContext context, Exception exception);
}
