using Meran.Back.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Meran.Back.Services;

    public interface ITokenService
    {
        string GenerateAccessToken(Administrator user);
        string GenerateMachineAccessToken(string machineSubject, string role, int expiresMinutes);
        string GenerateApplicationUserAccessToken(ApplicationUser user, string planName, Dictionary<string, string> features);
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

        public string GenerateApplicationUserAccessToken(ApplicationUser user, string planName, Dictionary<string, string> features)
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_options.SigningKey));
            var credentials = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
                {
                    new("sub",              user.Id.ToString()),
                    new("application_user_id", user.Id.ToString()),
                    new("email",            user.Email),
                    new("application_id",   user.ApplicationId.ToString()),
                    new("jti",              Guid.NewGuid().ToString()),
                    new("plan_name",        planName),
                    new("plan_features",    JsonSerializer.Serialize(features))
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

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    public DateTime GetAccessTokenExpirationUtc()
        {
            return DateTime.UtcNow.AddMinutes(_options.AccessTokenExpiresMinutes);
        }
    }

