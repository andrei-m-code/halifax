using Halifax.Core.Helpers;

namespace Halifax.Core.Tests.Helpers;

public class CryptoTests
{
    private const string secret = "Crypto.Secret.123qweRTY123123ZZZXXXYYYCCC11122233344455";
    private const string encrypted = "HV56ya6qMh3B46jH24ddl4bJl9V/4rawmdFThpR9bcY=";
    private const string plain = "Crypto#Test@.1";

    [Test]
    public void EncryptTest()
    {
        var encryptedResult = Crypto.Encrypt(secret, plain);
        Assert.That(encryptedResult, Is.EqualTo(encrypted));
    }

    [Test]
    public void DecryptTest()
    {
        var decryptedResult = Crypto.Decrypt(secret, encrypted);
        Assert.That(decryptedResult, Is.EqualTo(plain));
    }
}