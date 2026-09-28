using System.Security.Cryptography;

namespace MovieLogger.Service.Security
{
    /// <summary>
    /// PBKDF2-SHA256 password hashing with a per-password random salt. Avoids taking a dependency on
    /// ASP.NET Core Identity purely for its PasswordHasher; this is the same underlying algorithm.
    /// </summary>
    public class Pbkdf2PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 16;
        private const int SubkeySize = 32;
        private const int Iterations = 100_000;
        private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

        public string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var subkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, SubkeySize);

            return string.Join(
                '.',
                Iterations.ToString(),
                Convert.ToBase64String(salt),
                Convert.ToBase64String(subkey));
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            var parts = passwordHash.Split('.');
            if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
            {
                return false;
            }

            byte[] salt;
            byte[] expectedSubkey;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expectedSubkey = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actualSubkey = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedSubkey.Length);
            return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
        }
    }
}
