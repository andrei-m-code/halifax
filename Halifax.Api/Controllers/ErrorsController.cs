using Halifax.Api.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Halifax.Api.Controllers;

[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorsController : ControllerBase
{
    private readonly IExceptionHandler exceptionHandler;

    public ErrorsController(IExceptionHandler exceptionHandler)
    {
        this.exceptionHandler = exceptionHandler;
    }

    [Route("error")]
    public async Task<object> Error()
    {
        var context = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var exception = context?.Error;

        var (response, code) = await exceptionHandler.HandleAsync(HttpContext, exception);

        Response.StatusCode = (int)code;

        return response;
    }
}
