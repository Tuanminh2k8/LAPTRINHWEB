namespace Source.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return string.Empty;

            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            try
            {
                // Fallback check if old hash format was SHA256 (64 hex characters)
                if (hash.Length == 64 && !hash.StartsWith("$2"))
                {
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        byte[] bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                        var builder = new System.Text.StringBuilder();
                        foreach (byte b in bytes)
                        {
                            builder.Append(b.ToString("X2"));
                        }
                        return string.Equals(builder.ToString(), hash, System.StringComparison.OrdinalIgnoreCase);
                    }
                }

                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }
    }
}
