using Halifax.Api.Filters;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using Halifax.Core.Extensions;

namespace PeggysCove.Api.Filters;

public class UserAuthorizeAttribute : ClaimsAuthorizeFilterAttribute
{
    protected override bool IsAuthorized(ActionExecutingContext context, List<Claim> claims)
    {
        claims.ClaimNotNullOrWhiteSpace(JwtTokenConstants.RoleClaim, out _);
        claims.ClaimIsGuid(JwtTokenConstants.IdClaim, out _);
        claims.ClaimIsEmail(JwtTokenConstants.EmailClaim, out _);

        return true;
    }
}
