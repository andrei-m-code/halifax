using Halifax.Domain.Exceptions;
using Halifax.Core.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Halifax.Api.Filters;

public abstract class ClaimsAuthorizeFilterAttribute : ActionFilterAttribute
{
    private const string unauthorizedMessage = "Request is unauthorized";
    
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var claims = context.HttpContext.User?.Claims?.ToList();

        if (!IsAuthorized(context, claims))
        {
            throw new HalifaxUnauthorizedException(unauthorizedMessage);
        }
    }
    
    protected void Expect(IEnumerable<Claim> claims, string claimType, string expectedValue)
    {
        claims.ClaimExpected(claimType, expectedValue);
    }
    
    protected abstract bool IsAuthorized(ActionExecutingContext context, List<Claim> claims);
}
