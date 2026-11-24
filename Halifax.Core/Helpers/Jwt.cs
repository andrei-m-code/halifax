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
    /// Creates a JWT signed with symmetric HMAC-SHA256 using the supplied secret.
    /// </summary>
    /// <param name="secret">The signing secret. Must be at least 16 characters.</param>
    /// <param name="claims">The claims to include in the token.</param>
    /// <param name="expiration">The absolute token expiration time.</param>
    /// <returns>The serialized (compact) JWT string.</returns>
    /// <exception cref="HalifaxException">Thrown when <paramref name="secret"/> is <see langword="null"/> or shorter than 16 characters.</exception>
    /// <example>
    /// <code>
    /// var token = Jwt.Create(secret, new[] { new Claim("sub", userId) }, DateTime.UtcNow.AddHours(1));
    /// </code>
    /// </example>
    public static string Create(string secret, IEnumerable<Claim> claims, DateTime expiration)
    {
        Guard.NotNull(secret, nameof(secret));
        Guard.Length(secret, nameof(secret), lower: 16);

        // ASCII bytes, as in every earlier version, so services that validate with them keep accepting new tokens.
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
    /// Reads and validates a JWT, returning its claims.
    /// </summary>
    /// <param name="secret">The signing secret used to validate the token.</param>
    /// <param name="jwt">The JWT string to read.</param>
    /// <param name="throwUnauthorized">Whether to throw on invalid tokens. If <see langword="false"/>, an empty list is returned instead.</param>
    /// <param name="validateAudience">Whether to validate the audience claim.</param>
    /// <param name="validateIssuer">Whether to validate the issuer claim.</param>
    /// <param name="validateLifetime">Whether to validate the token expiration.</param>
    /// <returns>The token's claims, or an empty list when the token is invalid and <paramref name="throwUnauthorized"/> is <see langword="false"/>.</returns>
    /// <exception cref="HalifaxUnauthorizedException">Thrown when the token is invalid, expired, or fails validation and <paramref name="throwUnauthorized"/> is <see langword="true"/>.</exception>
    /// <remarks>Validation failures are logged as warnings via <see cref="L"/> before the method throws or returns an empty list.</remarks>
    /// <example>
    /// <code>
    /// var claims = Jwt.Read(secret, token, validateLifetime: true);
    /// </code>
    /// </example>
    public static List<Claim> Read(string secret, string jwt,
        bool throwUnauthorized = true,
        bool validateAudience = false,
        bool validateIssuer = false,
        bool validateLifetime = false)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();

            handler.ValidateToken(jwt, new TokenValidationParameters
            {
                IssuerSigningKeys = GetSigningKeys(secret),
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

    /// <summary>
    /// The keys a token for <paramref name="secret"/> is validated against. <see cref="Create"/> signs with the secret's
    /// ASCII bytes (every non-ASCII character becomes <c>?</c>), while <c>ConfigureAuthentication</c> and most other JWT
    /// code use its UTF-8 bytes. For an ASCII secret those are the same bytes and one key is returned; for a secret
    /// with non-ASCII characters both keys are returned, so tokens from either side validate in <see cref="Read"/>
    /// and in the authentication middleware.
    /// </summary>
    /// <param name="secret">The signing secret.</param>
    /// <returns>One key for an ASCII secret; the ASCII and UTF-8 keys for a secret with non-ASCII characters.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="secret"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<SecurityKey> GetSigningKeys(string secret)
    {
        ArgumentNullException.ThrowIfNull(secret);

        var ascii = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret));
        return Ascii.IsValid(secret)
            ? [ascii]
            : [ascii, new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))];
    }
}
