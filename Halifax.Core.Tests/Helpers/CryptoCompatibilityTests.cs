using System.Security.Cryptography;
using System.Text;
using Halifax.Core.Helpers;

namespace Halifax.Core.Tests.Helpers;

/// <summary>
/// Values that services already store were written by Halifax 6.0.0 and earlier. These golden values were produced
/// by the 6.0.0 code and must keep decrypting, and the legacy format must keep producing exactly the same output,
/// for as long as the library exists.
/// </summary>
[NonParallelizable]
public class CryptoCompatibilityTests
{
    private static readonly (string Secret, string Text, string Encrypted)[] golden =
    [
        ("my-secret", "hello", "5+G/hDAq0ZTzf3Rbw5RaJg=="),
        ("Sup3r$ecret-Key_With-Symbols!", "user@example.com", "NfDlPBV8jLg/9E4Fwg1gX7YwHGm0nPWAHmughWvseOeXYeGzycBzR6ycpQkTUhKq"),
        ("short", "", "bQj7NU92h+HMisIqTs5Bdg=="),
        ("ünïcødé-sécret-∑", "Ünïcødé text ✓ 日本", "LncsDuTd7eWKIPfbmRMxRwfZRYDynn3KXKt6Wg6L4dafh4dhh9KYsOFXKo6tMxcV"),
    ];

    // 6.0.0 output for a 1000-character text, kept as length + SHA-256 to keep the file readable.
    private const string longSecret = "0123456789abcdef0123456789abcdef";
    private const int longLength = 2688;
    private const string longSha256 = "be2008c1155b37d48ee96b1fe141cc3d1e12682f42931393db54358b57b03f49";

    [Test]
    public void Legacy_Encrypt_MatchesHalifax600ByteForByte()
    {
        foreach (var (secret, text, encrypted) in golden)
        {
            Assert.That(Crypto.Encrypt(secret, text), Is.EqualTo(encrypted), secret);
            Assert.That(Crypto.Encrypt(secret, text, CryptoFormat.Legacy), Is.EqualTo(encrypted), secret);
        }

        var longValue = Crypto.Encrypt(longSecret, new string('x', 1000));
        Assert.That(longValue, Has.Length.EqualTo(longLength));
        Assert.That(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(longValue))), Is.EqualTo(longSha256));
    }

    [Test]
    public void Decrypt_ReadsHalifax600Values()
    {
        foreach (var (secret, text, encrypted) in golden)
        {
            Assert.That(Crypto.Decrypt(secret, encrypted), Is.EqualTo(text), secret);
            Assert.That(Crypto.TryDecrypt(secret, encrypted, out var result), Is.True);
            Assert.That(result, Is.EqualTo(text));
            Assert.That(Crypto.GetFormat(encrypted), Is.EqualTo(CryptoFormat.Legacy));
        }
    }

    [Test]
    public void Legacy_WrongSecret_FailsAsBefore()
    {
        Assert.Throws(Is.InstanceOf<CryptographicException>(), () => Crypto.Decrypt("wrong-secret", golden[1].Encrypted));
        Assert.That(Crypto.TryDecrypt("wrong-secret", golden[1].Encrypted, out _), Is.False);
        Assert.Throws<FormatException>(() => Crypto.Decrypt("my-secret", "not base64!"));
    }

    [Test]
    public void NullArguments_ThrowArgumentNullException([Values] CryptoFormat format)
    {
        Assert.Throws<ArgumentNullException>(() => Crypto.Encrypt(null!, "text", format));
        Assert.Throws<ArgumentNullException>(() => Crypto.Encrypt("secret", null!, format));
        Assert.Throws<ArgumentNullException>(() => Crypto.Decrypt(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => Crypto.Decrypt("secret", null!));
        Assert.That(Crypto.TryDecrypt("secret", null!, out _), Is.False);
    }
}

public class CryptoAesGcmTests
{
    private const string secret = "an-application-secret-for-tests";

    [TestCase("hello")]
    [TestCase("")]
    [TestCase("Ünïcødé text ✓ 日本")]
    public void RoundTrips(string text)
    {
        var encrypted = Crypto.Encrypt(secret, text, CryptoFormat.AesGcm);

        Assert.That(encrypted, Does.StartWith("v2."));
        Assert.That(Crypto.GetFormat(encrypted), Is.EqualTo(CryptoFormat.AesGcm));
        Assert.That(Crypto.Decrypt(secret, encrypted), Is.EqualTo(text));
    }

    [Test]
    public void SameText_GivesDifferentValues()
    {
        var first = Crypto.Encrypt(secret, "same text", CryptoFormat.AesGcm);
        var second = Crypto.Encrypt(secret, "same text", CryptoFormat.AesGcm);

        Assert.That(first, Is.Not.EqualTo(second));
        Assert.That(Crypto.Decrypt(secret, first), Is.EqualTo(Crypto.Decrypt(secret, second)));
    }

    [Test]
    public void Output_IsUrlSafe()
    {
        var encrypted = Crypto.Encrypt(secret, new string('?', 300), CryptoFormat.AesGcm);

        Assert.That(encrypted[3..], Does.Match("^[A-Za-z0-9_-]+$"));
    }

    [Test]
    public void AlteredValue_FailsToDecrypt()
    {
        var encrypted = Crypto.Encrypt(secret, "transfer 100", CryptoFormat.AesGcm);
        var flipped = encrypted[..^2] + (encrypted[^2] == 'A' ? 'B' : 'A') + encrypted[^1];

        Assert.Throws(Is.InstanceOf<CryptographicException>(), () => Crypto.Decrypt(secret, flipped));
        Assert.Throws(Is.InstanceOf<CryptographicException>(), () => Crypto.Decrypt(secret, "v2.AAAA"));
        Assert.That(Crypto.TryDecrypt(secret, flipped, out _), Is.False);
    }

    [Test]
    public void WrongSecret_FailsToDecrypt()
    {
        var encrypted = Crypto.Encrypt(secret, "hello", CryptoFormat.AesGcm);

        Assert.Throws(Is.InstanceOf<CryptographicException>(), () => Crypto.Decrypt("another-secret", encrypted));
    }

    [Test]
    public void TwoArgumentEncrypt_StaysDeterministic()
    {
        // Services check passwords by encrypting them and comparing with the stored value.
        var first = Crypto.Encrypt(secret, "same text");
        var second = Crypto.Encrypt(secret, "same text");
        Assert.That(second, Is.EqualTo(first));
        Assert.That(Crypto.GetFormat(Crypto.Encrypt(secret, "x")), Is.EqualTo(CryptoFormat.Legacy));
    }

    [Test]
    public void ManyDifferentSecrets_StillWork()
    {
        // More secrets than the key cache holds.
        for (var i = 0; i < 70; i++)
        {
            var s = $"secret-{i}";
            Assert.That(Crypto.Decrypt(s, Crypto.Encrypt(s, "x", CryptoFormat.AesGcm)), Is.EqualTo("x"));
        }
    }

    [Test]
    public void UnknownFormat_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Crypto.Encrypt(secret, "x", (CryptoFormat)42));
    }
}
