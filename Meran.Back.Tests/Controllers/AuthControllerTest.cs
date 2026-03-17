using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Meran.Back.Controllers;
using Meran.Back.Data;
using Meran.Back.DTO;
using Meran.Back.Models;
using Meran.Back.Services;
using Moq;

namespace Meran.Back.Tests.Controllers;

public class AuthControllerTest
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static string HashPassword(string password)
    {
        var salt = "static_salt_for_now";
        var bytes = System.Text.Encoding.UTF8.GetBytes(password + salt);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    [Test]
    public async Task Login_ReturnsUnauthorized_WhenUserDoesNotExist()
    {
        using var context = CreateInMemoryDbContext();
        var tokenServiceMock = new Mock<ITokenService>(MockBehavior.Strict);
        var controller = new AuthController(context, tokenServiceMock.Object);

        var request = new LoginRequestDto
        {
            Email = "unknown@example.com",
            Password = "password"
        };

        var result = await controller.Login(request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsInvalid()
    {
        using var context = CreateInMemoryDbContext();
        var tokenServiceMock = new Mock<ITokenService>(MockBehavior.Strict);
        var controller = new AuthController(context, tokenServiceMock.Object);

        var admin = new Administrator
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            PasswordHash = HashPassword("correct-password"),
            DisplayName = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Administrators.Add(admin);
        await context.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            Email = "admin@example.com",
            Password = "wrong-password"
        };

        var result = await controller.Login(request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task Login_ReturnsForbid_WhenUserIsInactive()
    {
        using var context = CreateInMemoryDbContext();
        var tokenServiceMock = new Mock<ITokenService>(MockBehavior.Strict);
        var controller = new AuthController(context, tokenServiceMock.Object);

        var admin = new Administrator
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            PasswordHash = HashPassword("password"),
            DisplayName = "Admin",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Administrators.Add(admin);
        await context.SaveChangesAsync();

        var request = new LoginRequestDto
        {
            Email = "admin@example.com",
            Password = "password"
        };

        var result = await controller.Login(request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ForbidResult>());
    }

    [Test]
    public async Task Login_ReturnsOk_WhenCredentialsAreValid()
    {
        using var context = CreateInMemoryDbContext();
        var tokenServiceMock = new Mock<ITokenService>(MockBehavior.Strict);

        var admin = new Administrator
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            PasswordHash = HashPassword("password"),
            DisplayName = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Administrators.Add(admin);
        await context.SaveChangesAsync();

        tokenServiceMock
            .Setup(s => s.GenerateAccessToken(admin))
            .Returns("test-token");

        var expectedExpiry = DateTime.UtcNow.AddHours(1);

        tokenServiceMock
            .Setup(s => s.GetAccessTokenExpirationUtc())
            .Returns(expectedExpiry);

        var controller = new AuthController(context, tokenServiceMock.Object);

        var request = new LoginRequestDto
        {
            Email = "admin@example.com",
            Password = "password"
        };

        var result = await controller.Login(request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.InstanceOf<AuthResponseDto>());

        var response = (AuthResponseDto)ok.Value!;
        Assert.That(response.AccessToken, Is.EqualTo("test-token"));
        Assert.That(response.ExpiresAtUtc, Is.EqualTo(expectedExpiry));
        Assert.That(response.User.Email, Is.EqualTo(admin.Email));
        Assert.That(response.User.DisplayName, Is.EqualTo(admin.DisplayName));

        tokenServiceMock.VerifyAll();
    }
}

