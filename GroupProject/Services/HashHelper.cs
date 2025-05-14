using System.Security.Cryptography;

namespace GroupProject.Services
{
    public static class HashHelper
    {
        public static string HashPassword(string password)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(password, 16, 10000, HashAlgorithmName.SHA256);
            byte[] salt = deriveBytes.Salt;
            byte[] hash = deriveBytes.GetBytes(32);

            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            try
            {
                var parts = storedHash.Split('.');
                if (parts.Length != 2)
                    return false;

                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] expectedHash = Convert.FromBase64String(parts[1]);

                using var deriveBytes = new Rfc2898DeriveBytes(enteredPassword, salt, 10000, HashAlgorithmName.SHA256);
                byte[] actualHash = deriveBytes.GetBytes(32);

                return actualHash.SequenceEqual(expectedHash);
            }
            catch
            {
                return false;
            }
        }
    }
}
