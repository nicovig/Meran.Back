using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Meran.Back.Services
{
    public class PasswordOptions
    {
        public string Pepper { get; set; } = null!;
        public int Iterations { get; set; } = 120000;
        public int HashSize { get; set; } = 32;
    }

    public interface IPasswordService
    {
        string HashPassword(string password, string salt);
        bool VerifyPassword(string password, string passwordHash, string salt);
    }

    public class PasswordService : IPasswordService
    {
        private readonly PasswordOptions _options;

        public PasswordService(IOptions<PasswordOptions> options)
        {
            _options = options.Value;
        }

        public string HashPassword(string password, string salt)
        {
            var saltBytes = BuildSaltBytes(salt);
            var hashBytes = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, _options.Iterations, HashAlgorithmName.SHA256, _options.HashSize);
            return Convert.ToBase64String(hashBytes);
        }

        public bool VerifyPassword(string password, string passwordHash, string salt)
        {
            if (string.IsNullOrWhiteSpace(salt) || string.IsNullOrWhiteSpace(passwordHash) || string.IsNullOrWhiteSpace(_options.Pepper))
            {
                return false;
            }

            byte[] storedHash;
            try
            {
                storedHash = Convert.FromBase64String(passwordHash);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] saltBytes;
            try
            {
                saltBytes = BuildSaltBytes(salt);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            var computedHash = Rfc2898DeriveBytes.Pbkdf2(password, saltBytes, _options.Iterations, HashAlgorithmName.SHA256, storedHash.Length);
            return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
        }

        private byte[] BuildSaltBytes(string salt)
        {
            return Encoding.UTF8.GetBytes($"{salt}:{_options.Pepper}");
        }
    }
}
