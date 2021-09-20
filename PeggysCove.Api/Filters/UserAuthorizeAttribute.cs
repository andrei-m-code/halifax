using Halifax.Api.Filters;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace PeggysCove.Api.Filters
{
    public class UserAuthorizeAttribute : ClaimsAuthorizeFilterAttribute
    {
        protected override bool IsAuthorized(ActionExecutingContext context, List<Claim> claims)
        {
            var role = claims.FirstOrDefault(c => c.Type == JwtTokenConstants.RoleClaim)?.Value;
            var idString = claims.FirstOrDefault(c => c.Type == JwtTokenConstants.IdClaim)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == JwtTokenConstants.NameClaim)?.Value;
            var authenticate = role == JwtTokenConstants.UserRoleClaim
                && Guid.TryParse(idString, out _)
                && !string.IsNullOrWhiteSpace(name);

            return authenticate;
        }
    }
}