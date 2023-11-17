using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Halifax.Api.Errors;

public interface IHalifaxExceptionHandler : IExceptionHandler
{
    Task<(object Response, HttpStatusCode Code)> HandleAsync(HttpContext context, Exception exception);
}
