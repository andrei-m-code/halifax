using Halifax.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Halifax.Api.Filters;

public abstract class ClaimsAuthorizeFilterAttribute : ActionFilterAttribute
{
    private const string unauthorizedMessage = "Request is unauthorized";
    
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var claims = context.HttpContext.User?.Claims?.ToList();

        if (claims == null || !IsAuthorized(context, claims))
        {
            throw new HalifaxUnauthorizedException(unauthorizedMessage);
        }
    }

    protected void Expect(IEnumerable<Claim> claims, string claimKey, string expectedValue)
    {
        if (claims?.FirstOrDefault(c => c.Type == claimKey)?.Value != expectedValue)
        {
            throw new HalifaxUnauthorizedException(unauthorizedMessage);
        }
    }
    
    protected abstract bool IsAuthorized(ActionExecutingContext context, List<Claim> claims);
}
