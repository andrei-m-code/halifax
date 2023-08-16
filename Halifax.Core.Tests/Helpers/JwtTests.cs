using System.Security.Claims;
using Halifax.Core.Helpers;

namespace Halifax.Core.Tests.Helpers;

public class JwtTests
{
    private const string secret = "JWT-Secret-123qweRTY123123ZZZXXXYYYCCC11122233344455";
    private readonly List<Claim> claims = new()
    {
        new("Test1", "Hello"),
        new("Test2", "World")
    };

    [Test]
    public void CreateAndReadJwt()
    {
        var expiration = DateTimeOffset.Now.AddYears(1);
        var createdJwt = Jwt.Create(secret, claims, expiration.DateTime);
        var actualClaims = Jwt.Read(secret, createdJwt);
        Assert.That(actualClaims, Is.Not.Null);
        VerifyClaims(claims, actualClaims, expiration);
    }

    [Test]
    public void ReadExistingJwt()
    {
        var jwt = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJUZXN0MSI6IkhlbGxvIiwiVGVzdDIiOiJXb3JsZCIsIm5iZiI6MTY4ODkyOTUzNywiZXhwIjoxNjg4OTQzOTQyLCJpYXQiOjE2ODg5Mjk1Mzd9.o7svk12AG8SYoTdLM3HhOa0d8nXjWHumIrtRTaWoVcc";
        var jwtExpiration = DateTimeOffset.FromUnixTimeSeconds(1688943942); 
        var readClaims = Jwt.Read(secret, jwt, validateLifetime: false);
        VerifyClaims(claims, readClaims, jwtExpiration);
    }

    private void VerifyClaims(List<Claim> expected, List<Claim> actual, DateTimeOffset expiration)
    {
        var anyMissing = expected.Any(exp => actual.FirstOrDefault(a => a.Type == exp.Type)?.Value != exp.Value);
        var expirationMatching = actual.First(a => a.Type == "exp").Value == expiration.ToUnixTimeSeconds().ToString();
        Assert.That(expirationMatching, Is.True);
        Assert.That(anyMissing, Is.False);
    }
}