using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Halifax.Core.Helpers
{
    public static class Crypto
    {
        private static readonly byte[] salt = { 0x41, 0x76, 0x62, 0x6e, 0x20, 0x4d, 0x55, 0x64, 0x76, 0x65, 0x54, 0x69, 0x66 };

        public static string Encrypt(string secret, string text)
        {
            using var encryptor = Aes.Create();
            using var ms = new MemoryStream();

            var clearBytes = Encoding.Unicode.GetBytes(text);
            var pdb = new Rfc2898DeriveBytes(secret, salt);

            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);

            using var cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(clearBytes, 0, clearBytes.Length);

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string secret, string encrypted)
        {
            using var encryptor = Aes.Create();
            using var ms = new MemoryStream();

            var cipherBytes = Convert.FromBase64String(encrypted);
            var pdb = new Rfc2898DeriveBytes(secret, salt);

            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);

            using var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(cipherBytes, 0, cipherBytes.Length);

            return Encoding.Unicode.GetString(ms.ToArray());
        }

        public static bool TryDecrypt(string secret, string encrypted, out string result)
        {
            try
            {
                result = Decrypt(secret, encrypted);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}
