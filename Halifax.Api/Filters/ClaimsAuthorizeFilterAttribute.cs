using Halifax.Core.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Halifax.Api.Filters;

public abstract class ClaimsAuthorizeFilterAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var claims = context.HttpContext.User?.Claims?.ToList();

        if (claims == null || !IsAuthorized(context, claims))
        {
            throw new HalifaxUnauthorizedException("Request is unauthorized");
        }
    }

    protected abstract bool IsAuthorized(ActionExecutingContext context, List<Claim> claims);
}
