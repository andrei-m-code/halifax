using System.Security.Cryptography;
using System.Text;

namespace Halifax.Core.Helpers;

/// <summary>
/// AES encryption and decryption utilities. Keys and IVs are derived from a secret with PBKDF2
/// (a fixed salt and iteration count), and text is encrypted with AES-CBC and PKCS7 padding.
/// </summary>
/// <remarks>
/// The same <c>secret</c> must be used to encrypt and decrypt a value. Key derivation uses the
/// obsolete <see cref="Rfc2898DeriveBytes"/> constructor (raising <c>SYSLIB0060</c>) to preserve
/// backward compatibility with previously encrypted values.
/// </remarks>
public static class Crypto
{
    private static readonly HashAlgorithmName algorithm = HashAlgorithmName.SHA1;
    private static readonly byte[] salt = "Avbn MUdveTif"u8.ToArray();
    private const int iterations = 1000;

    /// <summary>Encrypts text using AES-CBC with PKCS7 padding.</summary>
    /// <param name="secret">The secret from which the encryption key and IV are derived.</param>
    /// <param name="text">The plaintext to encrypt.</param>
    /// <returns>Base64-encoded ciphertext that can be reversed with <see cref="Decrypt"/> using the same <paramref name="secret"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="secret"/> or <paramref name="text"/> is <see langword="null"/>.</exception>
    /// <example>
    /// <code>
    /// var cipher = Crypto.Encrypt("my-secret", "hello");
    /// var plain = Crypto.Decrypt("my-secret", cipher); // "hello"
    /// </code>
    /// </example>
    public static string Encrypt(string secret, string text)
    {
        var clearBytes = Encoding.Unicode.GetBytes(text);
        using var encryptor = CreateAes();
        var pdb = new Rfc2898DeriveBytes(secret, salt, iterations, algorithm);
        encryptor.Key = pdb.GetBytes(32);
        encryptor.IV = pdb.GetBytes(16);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
        {
            cs.Write(clearBytes, 0, clearBytes.Length);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>Decrypts a Base64-encoded AES-CBC ciphertext produced by <see cref="Encrypt"/>.</summary>
    /// <param name="secret">The secret used when the value was encrypted.</param>
    /// <param name="encrypted">The Base64-encoded ciphertext.</param>
    /// <returns>The decrypted plaintext.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="encrypted"/> is not a valid Base64 string.</exception>
    /// <exception cref="CryptographicException">Thrown when the ciphertext cannot be decrypted, typically because the wrong <paramref name="secret"/> was supplied.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="secret"/> or <paramref name="encrypted"/> is <see langword="null"/>.</exception>
    /// <seealso cref="TryDecrypt"/>
    public static string Decrypt(string secret, string encrypted)
    {
        var cipherBytes = Convert.FromBase64String(encrypted);

        using var encryptor = CreateAes();
        var pdb = new Rfc2898DeriveBytes(secret, salt, iterations, algorithm);

        encryptor.Key = pdb.GetBytes(32);
        encryptor.IV = pdb.GetBytes(16);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
        {
            cs.Write(cipherBytes, 0, cipherBytes.Length);
        }

        return Encoding.Unicode.GetString(ms.ToArray());
    }

    /// <summary>Attempts to decrypt a value, returning <see langword="false"/> on failure instead of throwing.</summary>
    /// <param name="secret">The secret used when the value was encrypted.</param>
    /// <param name="encrypted">The Base64-encoded ciphertext.</param>
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

    private static Aes CreateAes()
    {
        var encryptor = Aes.Create();
        encryptor.Mode = CipherMode.CBC;
        encryptor.Padding = PaddingMode.PKCS7;
        
        return encryptor;
    }
}
