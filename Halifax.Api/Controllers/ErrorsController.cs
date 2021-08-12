using Halifax.Core.Exceptions;
using Halifax.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Halifax.Api.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorsController : ControllerBase
    {
        [Route("error")]
        public ApiResponse Error()
        {
            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
            var exception = context?.Error;

            var code = exception switch
            {
                HalifaxNotFoundException => HttpStatusCode.NotFound,
                HalifaxUnauthorizedException => HttpStatusCode.Unauthorized,
                HalifaxException => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.InternalServerError
            };

            Response.StatusCode = (int)code;

            return ApiResponse.With(exception);
        }
    }
}