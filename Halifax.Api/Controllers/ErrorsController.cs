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
        //var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
        var context = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var exception = context?.Error;

        await LogRequestAsync(context);

        var (response, code) = await exceptionHandler.HandleAsync(exception);

        Response.StatusCode = (int)code;

        return response;
    }

    private async Task LogRequestAsync(IExceptionHandlerPathFeature context)
    {
        var path = context.Path;
        var body = Request.Body;
        var bodyString = string.Empty;

        if (Request.QueryString.HasValue)
        {
            path += Request.QueryString.Value;
        }

        if (body.CanRead)
        {
            body.Position = 0;
            using var stream = new StreamReader(Request.Body);
            bodyString = await stream.ReadToEndAsync();
        }

        L.Info($"Error happened when processing the request \r\n{Request.Method}: {path}\r\nBODY: {bodyString}");
    }
}
