using Halifax.Core.Exceptions;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Halifax.Api.Filters
{
    public abstract class ClaimsFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var claims = context.HttpContext.User?.Claims?.ToList();

            if (claims == null || !IsAuthorize(claims))
            {
                throw new HalifaxUnauthorizedException("Request is unauthorized");
            }
        }

        protected abstract bool IsAuthorize(List<Claim> claims);
    }
}