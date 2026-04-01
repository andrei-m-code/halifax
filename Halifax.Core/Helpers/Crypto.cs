using System.Security.Cryptography;
using System.Text;

namespace Halifax.Core.Helpers;

/// <summary>
/// AES encryption and decryption utilities.
/// </summary>
public static class Crypto
{
    private static readonly HashAlgorithmName algorithm = HashAlgorithmName.SHA1;
    private static readonly byte[] salt = "Avbn MUdveTif"u8.ToArray();
    private const int iterations = 1000;

    /// <summary>Encrypts text using AES-CBC with PKCS7 padding.</summary>
    /// <param name="secret">The encryption key.</param>
    /// <param name="text">The plaintext to encrypt.</param>
    /// <returns>Base64-encoded ciphertext.</returns>
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

    /// <summary>Decrypts a Base64-encoded AES-CBC ciphertext.</summary>
    /// <param name="secret">The encryption key.</param>
    /// <param name="encrypted">The Base64-encoded ciphertext.</param>
    /// <returns>The decrypted plaintext.</returns>
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

    /// <summary>Attempts to decrypt. Returns false on failure instead of throwing.</summary>
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
