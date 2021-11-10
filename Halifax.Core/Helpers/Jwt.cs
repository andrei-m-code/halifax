using Halifax.Core.Exceptions;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Halifax.Core.Helpers;

public static class Jwt
{
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
        bool validateExpiration = false)
    {
        try
        {
            var key = Encoding.ASCII.GetBytes(secret);
            var handler = new JwtSecurityTokenHandler();

            handler.ValidateToken(jwt, new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateAudience = validateAudience,
                ValidateIssuer = validateIssuer
            }, out var token);

            var jwtToken = (JwtSecurityToken)token;

            if (!validateExpiration || token.ValidTo > DateTime.UtcNow)
            {
                return jwtToken.Claims.ToList();
            }
        }
        catch
        {
            // ignore
        }

        if (throwUnauthorized)
        {
            throw new HalifaxUnauthorizedException("Request is unauthorized");
        }

        return new List<Claim>();
    }
}
