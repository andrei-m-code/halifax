using Halifax.Domain.Exceptions;
using Halifax.Core.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Halifax.Api.Filters;

/// <summary>
/// Base action filter attribute for claims-based authorization. Subclass it and implement
/// <see cref="IsAuthorized"/> to decide, per action, whether the current user's claims permit the request.
/// </summary>
/// <remarks>
/// Apply the derived attribute to a controller or action. Before the action runs, <see cref="OnActionExecuting"/>
/// gathers the authenticated user's claims and calls <see cref="IsAuthorized"/>; returning
/// <see langword="false"/> throws a <see cref="HalifaxUnauthorizedException"/>, which the Halifax exception
/// handler translates to an HTTP 401 response. Use the <see cref="Expect"/> helper inside
/// <see cref="IsAuthorized"/> to assert individual claim values.
/// </remarks>
/// <example>
/// <code>
/// public class AdminOnlyAttribute : ClaimsAuthorizeFilterAttribute
/// {
///     protected override bool IsAuthorized(ActionExecutingContext context, List&lt;Claim&gt; claims) =>
///         claims.Any(c => c.Type == "role" &amp;&amp; c.Value == "admin");
/// }
/// </code>
/// </example>
public abstract class ClaimsAuthorizeFilterAttribute : ActionFilterAttribute
{
    private const string unauthorizedMessage = "Request is unauthorized";

    /// <summary>
    /// Runs before the action executes: collects the current user's claims and enforces
    /// <see cref="IsAuthorized"/>.
    /// </summary>
    /// <param name="context">The context for the executing action.</param>
    /// <exception cref="HalifaxUnauthorizedException">Thrown when <see cref="IsAuthorized"/> returns <see langword="false"/>.</exception>
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var claims = context.HttpContext.User?.Claims?.ToList() ?? [];

        if (!IsAuthorized(context, claims))
        {
            throw new HalifaxUnauthorizedException(unauthorizedMessage);
        }
    }
    
    /// <summary>
    /// Asserts that the claim set contains a claim of the given type with the expected value, throwing when it
    /// is missing or does not match. Intended as a helper for <see cref="IsAuthorized"/> implementations.
    /// </summary>
    /// <param name="claims">The claims to inspect.</param>
    /// <param name="claimType">The claim type to look for.</param>
    /// <param name="expectedValue">The value the claim is required to have.</param>
    protected void Expect(IEnumerable<Claim> claims, string claimType, string expectedValue)
    {
        claims.ClaimExpected(claimType, expectedValue);
    }

    /// <summary>
    /// When implemented in a derived class, determines whether the request is authorized based on the current
    /// user's claims.
    /// </summary>
    /// <param name="context">The context for the executing action.</param>
    /// <param name="claims">The authenticated user's claims (empty when the request is unauthenticated).</param>
    /// <returns><see langword="true"/> to allow the request; <see langword="false"/> to reject it as unauthorized.</returns>
    protected abstract bool IsAuthorized(ActionExecutingContext context, List<Claim> claims);
}
