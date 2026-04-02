using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Meran.Back.Models;

namespace Meran.Back.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(Administrator user);
        string GenerateMachineAccessToken(string machineSubject, string role, int expiresMinutes);
        DateTime GetAccessTokenExpirationUtc();
    }

    public class TokenService : ITokenService
    {
        private readonly JwtOptions _options;

        public TokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public string GenerateAccessToken(Administrator user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("jti", Guid.NewGuid().ToString()),
                new Claim("role", user.Role ?? "Admin")
            };

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(_options.AccessTokenExpiresMinutes);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: credentials);

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(token);
        }

        public string GenerateMachineAccessToken(string machineSubject, string role, int expiresMinutes)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim("sub", machineSubject),
                new Claim("client_id", machineSubject),
                new Claim("role", role),
                new Claim("jti", Guid.NewGuid().ToString())
            };

            var now = DateTime.UtcNow;
            var expires = now.AddMinutes(expiresMinutes);

            var token = new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: expires,
                signingCredentials: credentials);

            var handler = new JwtSecurityTokenHandler();
            return handler.WriteToken(token);
        }

        public DateTime GetAccessTokenExpirationUtc()
        {
            return DateTime.UtcNow.AddMinutes(_options.AccessTokenExpiresMinutes);
        }
    }
}

