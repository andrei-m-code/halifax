using Halifax.Domain.Exceptions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Halifax.Core.Helpers;

public static class Jwt
{
    public const string ExpirationClaimType = "exp";
    
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
