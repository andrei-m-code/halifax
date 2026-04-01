using Halifax.Domain.Exceptions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Halifax.Core.Helpers;

/// <summary>
/// Utility for creating and reading JWT tokens using symmetric HMAC-SHA256 signing.
/// </summary>
public static class Jwt
{
    /// <summary>The standard JWT expiration claim type.</summary>
    public const string ExpirationClaimType = "exp";

    /// <summary>
    /// Creates a signed JWT token.
    /// </summary>
    /// <param name="secret">The signing secret. Must be at least 16 characters.</param>
    /// <param name="claims">The claims to include in the token.</param>
    /// <param name="expiration">The token expiration time.</param>
    public static string Create(string secret, IEnumerable<Claim> claims, DateTime expiration)
    {
        Guard.NotNull(secret, nameof(secret));
        Guard.Length(secret, nameof(secret), lower: 16);

        var key = Encoding.ASCII.GetBytes(secret);
        var handler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = (JwtSecurityToken)handler.CreateToken(descriptor);

        return token.RawData;
    }

    /// <summary>
    /// Reads and validates a JWT token, returning its claims.
    /// </summary>
    /// <param name="secret">The signing secret used to validate the token.</param>
    /// <param name="jwt">The JWT string to read.</param>
    /// <param name="throwUnauthorized">Whether to throw on invalid tokens. If false, returns an empty list.</param>
    /// <param name="validateAudience">Whether to validate the audience claim.</param>
    /// <param name="validateIssuer">Whether to validate the issuer claim.</param>
    /// <param name="validateLifetime">Whether to validate the token expiration.</param>
    public static List<Claim> Read(string secret, string jwt,
        bool throwUnauthorized = true,
        bool validateAudience = false,
        bool validateIssuer = false,
        bool validateLifetime = false)
    {
        try
        {
            var key = Encoding.ASCII.GetBytes(secret);
            var handler = new JwtSecurityTokenHandler();

            handler.ValidateToken(jwt, new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateAudience = validateAudience,
                ValidateIssuer = validateIssuer,
                ValidateLifetime = validateLifetime
            }, out var token);

            var jwtToken = (JwtSecurityToken)token;

            if (!validateLifetime || token.ValidTo > DateTime.UtcNow)
            {
                return jwtToken.Claims.ToList();
            }
        }
        catch (Exception ex)
        {
            L.Warning(ex, "Error reading JWT");
        }

        return throwUnauthorized 
            ? throw new HalifaxUnauthorizedException("Request is unauthorized") 
            : [];
    }
}
