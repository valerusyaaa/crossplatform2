using System.Security.Cryptography;
using System.Text;

namespace crossplatform2.Services
{
    public class PasswordService
    {
        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            var hashToVerify = HashPassword(password);
            return hashToVerify == storedHash;
        }
    }
}