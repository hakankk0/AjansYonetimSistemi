using System;
using System.Security.Cryptography;

namespace AjansYonetim.Yardimcilar
{
    public static class SifrelemeYardimcisi
    {
        private const int KeySize = 64;
        private const int Iterations = 350000;
        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;

        public static (string hash, string salt) SifreHashle(string parola)
        {
            var saltBytes = RandomNumberGenerator.GetBytes(KeySize);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                System.Text.Encoding.UTF8.GetBytes(parola),
                saltBytes,
                Iterations,
                HashAlgorithm,
                KeySize);

            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        public static bool SifreDogrula(string parola, string hash, string salt)
        {
            if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
                return false;
                
            try
            {
                var saltBytes = Convert.FromBase64String(salt);
                var hashBytes = Rfc2898DeriveBytes.Pbkdf2(
                    System.Text.Encoding.UTF8.GetBytes(parola),
                    saltBytes,
                    Iterations,
                    HashAlgorithm,
                    KeySize);

                return Convert.ToBase64String(hashBytes) == hash;
            }
            catch
            {
                 return false;
            }
        }
    }
}
