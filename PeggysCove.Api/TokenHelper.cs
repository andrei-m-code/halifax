using System.Security.Claims;
using Halifax.Core.Helpers;

namespace PeggysCove.Api;

public static class TokenHelper
{
    // It should be in the settings of the app
    internal const string JwtSecret = "Test JWT Token (at least 16 chars)";
    
    public static string CreateToken()
    {
        var claims = new Claim[]
        {
            new(JwtTokenConstants.RoleClaim, JwtTokenConstants.UserRoleClaim),
            new(JwtTokenConstants.IdClaim,Guid.NewGuid().ToString()),
            new(JwtTokenConstants.NameClaim, "James Smith"),
            new(JwtTokenConstants.EmailClaim, "james@example.com")
        };

        return Jwt.Create(JwtSecret, claims, DateTime.UtcNow.AddYears(1));
    }
}