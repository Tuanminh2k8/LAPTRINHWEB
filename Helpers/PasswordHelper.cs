using System.Security.Cryptography;
using System.Text;

namespace Source.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("X2"));
                }
                return builder.ToString();
            }
        }

        public static bool VerifyPassword(string password, string hash)
        {
            string hashedInput = HashPassword(password);
            return string.Equals(hashedInput, hash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
