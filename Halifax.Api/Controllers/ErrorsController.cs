using System.Globalization;
using System.Text;
using Halifax.Api.Errors;
using Halifax.Core.Extensions;
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
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("Error happened when processing the request:");

        var path = $"{Request.Method}: {context.Path}";
        var body = Request.Body;
        var bodyString = string.Empty;

        if (Request.QueryString.HasValue)
        {
            path += Request.QueryString.Value;
        }

        messageBuilder.AppendLine(path);

        Request.Headers
            .Where(h => h.Key.StartsWith("X-", true, CultureInfo.InvariantCulture))
            .Each(h => messageBuilder.AppendLine($"{h.Key}: {h.Value.ToString()}"));

        if (body.CanRead)
        {
            body.Position = 0;
            using var stream = new StreamReader(Request.Body);
            bodyString = await stream.ReadToEndAsync();
            if (!string.IsNullOrWhiteSpace(bodyString))
            {
                if (bodyString.Length > 1000)
                {
                    bodyString = bodyString[..1000];
                }

                messageBuilder.AppendLine($"Body: {bodyString}");
            }
        }

        L.Info(messageBuilder.ToString());
    }
}
