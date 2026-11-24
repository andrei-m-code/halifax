using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Halifax.Core.Helpers;
using Halifax.Domain.Exceptions;
using Microsoft.IdentityModel.Tokens;

namespace Halifax.Core.Tests.Helpers;

/// <summary>
/// Jwt.Create signs with the secret's ASCII bytes while the authentication middleware validated with its UTF-8 bytes.
/// Both are the same bytes for an ASCII secret; for a non-ASCII secret Jwt.Create tokens never passed the middleware.
/// Signing stays as it was (services such as FleetConnect validate with the ASCII bytes themselves); validation now
/// accepts both. Tokens below were issued by the 6.0.0 code (expiry 2099) and must keep validating.
/// </summary>
public class JwtCompatibilityTests
{
    private const string asciiSecret = "ascii-secret-0123456789-abcdefghij";
    private const string nonAsciiSecret = "ünïcødé-sécret-0123456789-abcdefg";

    private const string asciiToken600 =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyLTQyIiwicm9sZSI6ImFkbWluIiwibmJmIjoxNzk1NTQyNjkyLCJleHAiOjQwNzA5MDg4MDAsImlhdCI6MTc5NTU0MjY5Mn0.49Ke3R5uKGTo1a1vEP4koFIpzkR-H5Mvj9hqJY7ut2k";

    private const string nonAsciiToken600 =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ1c2VyLTQyIiwicm9sZSI6ImFkbWluIiwibmJmIjoxNzk1NTQyNjkyLCJleHAiOjQwNzA5MDg4MDAsImlhdCI6MTc5NTU0MjY5Mn0.WllPL-hjV4hl4FdLFdUQD9I9r5tdUwi11FsrNiSiYEA";

    private static readonly Claim[] claims = [new("sub", "user-42"), new("role", "admin")];

    [TestCase(asciiSecret, asciiToken600)]
    [TestCase(nonAsciiSecret, nonAsciiToken600)]
    public void Read_AcceptsTokensIssuedByHalifax600(string secret, string token)
    {
        var read = Jwt.Read(secret, token, validateLifetime: true);

        Assert.That(read.Single(c => c.Type == "sub").Value, Is.EqualTo("user-42"));
        Assert.That(read.Single(c => c.Type == "role").Value, Is.EqualTo("admin"));
    }

    [TestCase(asciiSecret, asciiToken600)]
    [TestCase(nonAsciiSecret, nonAsciiToken600)]
    public void SigningKeys_AcceptTokensIssuedByHalifax600(string secret, string token)
    {
        // What ConfigureAuthentication hands the JwtBearer middleware.
        Assert.DoesNotThrow(() => Validate(token, Jwt.GetSigningKeys(secret)));
    }

    [TestCase(asciiSecret)]
    [TestCase(nonAsciiSecret)]
    public void NewTokens_StillValidateWithTheAsciiKey(string secret)
    {
        var token = Jwt.Create(secret, claims, DateTime.UtcNow.AddHours(1));

        // What Halifax 6.0.0's Jwt.Read and FleetConnect's own validator use: they keep accepting new tokens.
        Assert.DoesNotThrow(() => Validate(token, [new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret))]));
        Assert.DoesNotThrow(() => Validate(token, Jwt.GetSigningKeys(secret)));
    }

    [TestCase(asciiSecret)]
    [TestCase(nonAsciiSecret)]
    public void Read_AcceptsTokensSignedWithTheUtf8Key(string secret)
    {
        // e.g. signed by other code with Encoding.UTF8.GetBytes(secret), the way the middleware validates.
        var token = Sign(secret, Encoding.UTF8);

        Assert.That(Jwt.Read(secret, token).Single(c => c.Type == "sub").Value, Is.EqualTo("user-42"));
        Assert.DoesNotThrow(() => Validate(token, Jwt.GetSigningKeys(secret)));
    }

    [Test]
    public void SigningKeys_OneForAsciiTwoForNonAscii()
    {
        Assert.That(Jwt.GetSigningKeys(asciiSecret), Has.Count.EqualTo(1));
        Assert.That(Jwt.GetSigningKeys(nonAsciiSecret), Has.Count.EqualTo(2));
    }

    [Test]
    public void WrongSecret_IsUnauthorized()
    {
        Assert.Throws<HalifaxUnauthorizedException>(() => Jwt.Read("another-secret-0123456789-abcdefgh", asciiToken600));
        Assert.That(Jwt.Read("another-secret-0123456789-abcdefgh", asciiToken600, throwUnauthorized: false), Is.Empty);
    }

    [Test]
    public void GetSigningKeys_NullSecret_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Jwt.GetSigningKeys(null!));
    }

    private static string Sign(string secret, Encoding encoding)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(encoding.GetBytes(secret)), SecurityAlgorithms.HmacSha256Signature)
        });

        return handler.WriteToken(token);
    }

    private static void Validate(string token, IEnumerable<SecurityKey> keys) =>
        new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            IssuerSigningKeys = keys,
            ValidateIssuerSigningKey = true,
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateLifetime = true
        }, out _);
}
