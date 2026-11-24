using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Halifax.Core.Helpers;

/// <summary>
/// The formats <see cref="Crypto.Encrypt(string, string, CryptoFormat)"/> can write. <see cref="Crypto.Decrypt"/> reads both.
/// </summary>
public enum CryptoFormat
{
    /// <summary>
    /// The format every Halifax version has written: AES-256-CBC with the key and IV derived from the secret
    /// (PBKDF2-SHA1, 1000 iterations, fixed salt), UTF-16 text, Base64 output. Deterministic (the same text always
    /// gives the same value) and not authenticated. Readable by every Halifax version.
    /// </summary>
    Legacy = 0,

    /// <summary>
    /// AES-256-GCM with a random nonce: authenticated (a changed or forged value fails to decrypt) and different
    /// output every time. The key is derived from the secret with PBKDF2-SHA256 (600,000 iterations) once per
    /// secret and cached. UTF-8 text. Output is <c>v2.</c> followed by URL-safe Base64.
    /// Readable by Halifax.Core 6.2.0 and later only.
    /// </summary>
    AesGcm = 1
}

/// <summary>
/// Symmetric encryption of strings with a shared secret.
/// </summary>
/// <remarks>
/// <para><see cref="Decrypt"/> reads both <see cref="CryptoFormat"/>s, so values written by any Halifax version keep
/// decrypting. <see cref="Encrypt(string, string)"/> always writes <see cref="CryptoFormat.Legacy"/>, exactly as
/// earlier versions did, so upgrading Halifax changes nothing that is stored or compared.</para>
/// <para><see cref="CryptoFormat.AesGcm"/> is chosen per call with <see cref="Encrypt(string, string, CryptoFormat)"/>,
/// deliberately not by a global switch: code that compares encrypted values (for example checking a password by
/// encrypting it and comparing the result with what is stored) relies on the same text giving the same value, and
/// would stop working. Use it only for values that are decrypted and never compared, after every service that
/// decrypts them runs Halifax.Core 6.2.0 or later; AES-GCM values are also longer.</para>
/// </remarks>
public static class Crypto
{
    private static readonly byte[] legacySalt = "Avbn MUdveTif"u8.ToArray();
    private const int legacyIterations = 1000;

    private const string gcmPrefix = "v2.";
    private static readonly byte[] gcmAssociatedData = "Halifax.Crypto.v2"u8.ToArray();
    private static readonly byte[] gcmSalt = "Halifax.Crypto.AesGcm.v2"u8.ToArray();
    private const int gcmIterations = 600_000;
    private const int gcmNonceSize = 12;
    private const int gcmTagSize = 16;

    // Deriving an AES-GCM key is deliberately slow, so keys are cached per secret. The cap keeps a caller that
    // passes many different secrets from growing the cache without bound (those keys are derived on every call).
    private const int maxCachedKeys = 64;
    private static readonly ConcurrentDictionary<string, byte[]> gcmKeys = new(StringComparer.Ordinal);

    /// <summary>Encrypts text in the <see cref="CryptoFormat.Legacy"/> format, exactly as earlier versions did.</summary>
    /// <param name="secret">The secret to encrypt with; the same secret decrypts the value.</param>
    /// <param name="text">The plaintext to encrypt.</param>
    /// <returns>The encrypted value, reversible with <see cref="Decrypt"/> and the same <paramref name="secret"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="secret"/> or <paramref name="text"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var cipher = Crypto.Encrypt("my-secret", "hello");
    /// var plain = Crypto.Decrypt("my-secret", cipher); // "hello"
    /// </code>
    /// </example>
    public static string Encrypt(string secret, string text) => Encrypt(secret, text, CryptoFormat.Legacy);

    /// <summary>Encrypts text in the given format.</summary>
    /// <param name="secret">The secret to encrypt with; the same secret decrypts the value.</param>
    /// <param name="text">The plaintext to encrypt.</param>
    /// <param name="format">The format to write.</param>
    /// <returns>The encrypted value, reversible with <see cref="Decrypt"/> and the same <paramref name="secret"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="secret"/> or <paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="format"/> is not a defined <see cref="CryptoFormat"/>.</exception>
    public static string Encrypt(string secret, string text, CryptoFormat format)
    {
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentNullException.ThrowIfNull(text);

        return format switch
        {
            CryptoFormat.Legacy => EncryptLegacy(secret, text),
            CryptoFormat.AesGcm => EncryptGcm(secret, text),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown crypto format")
        };
    }

    /// <summary>Decrypts a value written by <see cref="Encrypt(string, string, CryptoFormat)"/> in either format.</summary>
    /// <param name="secret">The secret used when the value was encrypted.</param>
    /// <param name="encrypted">The encrypted value.</param>
    /// <returns>The decrypted plaintext.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="encrypted"/> is not valid Base64 for its format.</exception>
    /// <exception cref="CryptographicException">Thrown when the value cannot be decrypted, typically because the wrong
    /// <paramref name="secret"/> was supplied or (for <see cref="CryptoFormat.AesGcm"/>) the value was altered.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="secret"/> or <paramref name="encrypted"/> is <see langword="null"/>.</exception>
    /// <seealso cref="TryDecrypt"/>
    public static string Decrypt(string secret, string encrypted)
    {
        ArgumentNullException.ThrowIfNull(secret);
        ArgumentNullException.ThrowIfNull(encrypted);

        return GetFormat(encrypted) == CryptoFormat.AesGcm
            ? DecryptGcm(secret, encrypted)
            : DecryptLegacy(secret, encrypted);
    }

    /// <summary>Attempts to decrypt a value, returning <see langword="false"/> on failure instead of throwing.</summary>
    /// <param name="secret">The secret used when the value was encrypted.</param>
    /// <param name="encrypted">The encrypted value, in either format.</param>
    /// <param name="result">When this method returns <see langword="true"/>, the decrypted plaintext; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when decryption succeeded; otherwise <see langword="false"/>.</returns>
    /// <remarks>Any exception raised by <see cref="Decrypt"/> (bad Base64, wrong secret, etc.) is swallowed and reported as <see langword="false"/>.</remarks>
    public static bool TryDecrypt(string secret, string encrypted, out string result)
    {
        try
        {
            result = Decrypt(secret, encrypted);
            return true;
        }
        catch
        {
            result = null!;
            return false;
        }
    }

    /// <summary>Tells which format an encrypted value is in, e.g. to find values still to re-encrypt.</summary>
    /// <param name="encrypted">The encrypted value.</param>
    /// <returns><see cref="CryptoFormat.AesGcm"/> for values starting with <c>v2.</c>; otherwise <see cref="CryptoFormat.Legacy"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="encrypted"/> is <see langword="null"/>.</exception>
    public static CryptoFormat GetFormat(string encrypted)
    {
        ArgumentNullException.ThrowIfNull(encrypted);

        // Legacy values are standard Base64, which has no '.', so the prefix cannot be mistaken for one.
        return encrypted.StartsWith(gcmPrefix, StringComparison.Ordinal) ? CryptoFormat.AesGcm : CryptoFormat.Legacy;
    }

    #region Legacy (AES-256-CBC), byte for byte as in earlier versions

    private static string EncryptLegacy(string secret, string text)
    {
        var (key, iv) = LegacyKey(secret);
        using var aes = Aes.Create();
        aes.Key = key;

        return Convert.ToBase64String(aes.EncryptCbc(Encoding.Unicode.GetBytes(text), iv, PaddingMode.PKCS7));
    }

    private static string DecryptLegacy(string secret, string encrypted)
    {
        var cipherBytes = Convert.FromBase64String(encrypted);
        var (key, iv) = LegacyKey(secret);
        using var aes = Aes.Create();
        aes.Key = key;

        return Encoding.Unicode.GetString(aes.DecryptCbc(cipherBytes, iv, PaddingMode.PKCS7));
    }

    // Earlier versions read 32 key bytes and then 16 IV bytes from one PBKDF2 stream, which is the first
    // 48 bytes of the same derivation.
    private static (byte[] Key, byte[] Iv) LegacyKey(string secret)
    {
        var bytes = Rfc2898DeriveBytes.Pbkdf2(secret, legacySalt, legacyIterations, HashAlgorithmName.SHA1, 48);
        return (bytes[..32], bytes[32..]);
    }

    #endregion

    #region AES-256-GCM

    private static string EncryptGcm(string secret, string text)
    {
        var plaintext = Encoding.UTF8.GetBytes(text);
        var payload = new byte[gcmNonceSize + plaintext.Length + gcmTagSize];
        var nonce = payload.AsSpan(0, gcmNonceSize);
        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(GcmKey(secret), gcmTagSize);
        aes.Encrypt(
            nonce,
            plaintext,
            payload.AsSpan(gcmNonceSize, plaintext.Length),
            payload.AsSpan(gcmNonceSize + plaintext.Length, gcmTagSize),
            gcmAssociatedData);

        return gcmPrefix + Base64Url.EncodeToString(payload);
    }

    private static string DecryptGcm(string secret, string encrypted)
    {
        var payload = Base64Url.DecodeFromChars(encrypted.AsSpan(gcmPrefix.Length));
        if (payload.Length < gcmNonceSize + gcmTagSize)
        {
            throw new CryptographicException("The encrypted value is too short");
        }

        var cipherLength = payload.Length - gcmNonceSize - gcmTagSize;
        var plaintext = new byte[cipherLength];

        using var aes = new AesGcm(GcmKey(secret), gcmTagSize);
        aes.Decrypt(
            payload.AsSpan(0, gcmNonceSize),
            payload.AsSpan(gcmNonceSize, cipherLength),
            payload.AsSpan(gcmNonceSize + cipherLength, gcmTagSize),
            plaintext,
            gcmAssociatedData);

        return Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] GcmKey(string secret)
    {
        if (gcmKeys.TryGetValue(secret, out var key))
        {
            return key;
        }

        key = Rfc2898DeriveBytes.Pbkdf2(secret, gcmSalt, gcmIterations, HashAlgorithmName.SHA256, 32);
        if (gcmKeys.Count < maxCachedKeys)
        {
            gcmKeys.TryAdd(secret, key);
        }

        return key;
    }

    #endregion
}
