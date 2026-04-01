using Halifax.Domain.Exceptions;
using Halifax.Core.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Halifax.Api.Filters;

/// <summary>
/// Base action filter for claims-based authorization.
/// Subclass and implement <see cref="IsAuthorized"/> to define authorization logic.
/// </summary>
public abstract class ClaimsAuthorizeFilterAttribute : ActionFilterAttribute
{
    private const string unauthorizedMessage = "Request is unauthorized";
    
    /// <inheritdoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var claims = context.HttpContext.User?.Claims?.ToList() ?? [];

        if (!IsAuthorized(context, claims))
        {
            throw new HalifaxUnauthorizedException(unauthorizedMessage);
        }
    }
    
    /// <summary>Validates that a claim has the expected value. Throws on mismatch.</summary>
    protected void Expect(IEnumerable<Claim> claims, string claimType, string expectedValue)
    {
        claims.ClaimExpected(claimType, expectedValue);
    }
    
    /// <summary>Implement to define whether the request is authorized based on its claims.</summary>
    protected abstract bool IsAuthorized(ActionExecutingContext context, List<Claim> claims);
}
