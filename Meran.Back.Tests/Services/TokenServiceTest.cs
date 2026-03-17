using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Meran.Back.Models;
using Meran.Back.Services;

namespace Meran.Back.Tests.Services;

public class TokenServiceTest
{
    private static TokenService CreateService(int expiresMinutes = 60)
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "test-issuer",
            Audience = "test-audience",
            SigningKey = "super_secret_signing_key_1234567890",
            AccessTokenExpiresMinutes = expiresMinutes
        });

        return new TokenService(options);
    }

    [Test]
    public void GenerateAccessToken_ContainsExpectedClaims()
    {
        var service = CreateService();

        var userId = Guid.NewGuid();
        var user = new Administrator
        {
            Id = userId,
            Email = "admin@example.com",
            Role = "Admin"
        };

        var tokenString = service.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        Assert.That(token.Issuer, Is.EqualTo("test-issuer"));
        Assert.That(token.Audiences, Contains.Item("test-audience"));

        var sub = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        var email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        var role = token.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
        var jti = token.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;

        Assert.That(sub, Is.EqualTo(userId.ToString()));
        Assert.That(email, Is.EqualTo("admin@example.com"));
        Assert.That(role, Is.EqualTo("Admin"));
        Assert.That(string.IsNullOrWhiteSpace(jti), Is.False);
    }

    [Test]
    public void GenerateAccessToken_UsesDefaultRole_WhenRoleIsNull()
    {
        var service = CreateService();

        var user = new Administrator
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            Role = null
        };

        var tokenString = service.GenerateAccessToken(user);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(tokenString);

        var role = token.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

        Assert.That(role, Is.EqualTo("Admin"));
    }

    [Test]
    public void GetAccessTokenExpirationUtc_UsesConfiguredLifetime()
    {
        var expiresMinutes = 30;
        var service = CreateService(expiresMinutes);

        var before = DateTime.UtcNow;
        var expiration = service.GetAccessTokenExpirationUtc();
        var after = DateTime.UtcNow;

        var expectedMin = before.AddMinutes(expiresMinutes - 1);
        var expectedMax = after.AddMinutes(expiresMinutes + 1);

        Assert.That(expiration, Is.GreaterThanOrEqualTo(expectedMin));
        Assert.That(expiration, Is.LessThanOrEqualTo(expectedMax));
    }
}

