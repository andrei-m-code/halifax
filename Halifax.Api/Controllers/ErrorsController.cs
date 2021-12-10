using Halifax.Api.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Halifax.Api.Controllers;

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
        var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var exception = context?.Error;
        var (response, code) = await exceptionHandler.HandleAsync(exception);

        // CORS issues when exception happens for some reason...
        // This is a temporary (permanent) fix for now (forever)
        // Response.Headers.TryAdd("Access-Control-Allow-Origin", "*");
        // Response.Headers.TryAdd("Access-Control-Allow-Methods", "*");
        // Response.Headers.TryAdd("Access-Control-Allow-Headers", "*");
        // Response.Headers.TryAdd("Access-Control-Max-Age", "8640");

        Response.StatusCode = (int)code;

        return response;
    }
}
