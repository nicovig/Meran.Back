using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Meran.Back.Models;
using Meran.Back.Services;

namespace Meran.Back.Tests.Auth;

public class JwtRoleClaimValidationTest
{
    [Test]
    public void ValidatedAccessToken_PrincipalIsInAdminRole()
    {
        var signingKey = "super_secret_signing_key_1234567890";
        var options = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = signingKey,
            AccessTokenExpiresMinutes = 60
        });
        var service = new TokenService(options);

        var user = new Administrator
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            Role = "Admin"
        };

        var tokenString = service.GenerateAccessToken(user);

        var keyBytes = Encoding.UTF8.GetBytes(signingKey);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "test-issuer",
            ValidateAudience = true,
            ValidAudience = "test-audience",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = securityKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(tokenString, validationParameters, out _);

        Assert.That(principal.IsInRole("Admin"), Is.True);
    }
}
