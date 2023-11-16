using System.Security.Claims;
using Halifax.Core.Helpers;

namespace Halifax.Core.Tests.Helpers;

public class JwtTests
{
    [Test]
    public void CreateAndReadJwt()
    {
        var secret = "123qwe!@#,./*();zxcZXC0001234567";
        var expiration = DateTimeOffset.Now.AddYears(1);
        var expectedExpiration = expiration.ToUnixTimeSeconds();
        var claimKey1 = "Test1";
        var claimKey2 = "Test2";
        var claimValue1 = "Hello";
        var claimValue2 = "World";
        var claims = new List<Claim>
        {
            new(claimKey1, claimValue1),
            new(claimKey2, claimValue2)
        };

        var createdJwt = Jwt.Create(secret, claims, expiration.DateTime);
        var readClaims = Jwt.Read(secret, createdJwt);

        var test1 = readClaims.FirstOrDefault(c => c.Type == claimKey1);
        var test2 = readClaims.FirstOrDefault(c => c.Type == claimKey2);
        var expirationClaim = readClaims.FirstOrDefault(c => c.Type == "exp");
        var expirationValue = Convert.ToInt64(expirationClaim?.Value);
        
        Assert.That(test1, Is.Not.Null);
        Assert.That(test2, Is.Not.Null);
        Assert.That(test1?.Value, Is.EqualTo(claimValue1));
        Assert.That(test2?.Value, Is.EqualTo(claimValue2));
        Assert.That(expirationValue, Is.EqualTo(expectedExpiration));
    }
}