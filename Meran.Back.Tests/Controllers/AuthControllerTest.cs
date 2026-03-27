using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Meran.Back.Controllers;
using Meran.Back.Data;
using Meran.Back.DTO;
using Meran.Back.Models;
using Meran.Back.Services;
using Moq;
using Microsoft.Extensions.Options;

namespace Meran.Back.Tests.Controllers;

public class AuthControllerTest
{
    private const string SaltInvalidPassword = "MDEyMzQ1Njc4OWFiY2RlZg==";
    private const string SaltInactive = "MTIzNDU2Nzg5MGFiY2RlZg==";
    private const string SaltValid = "YWJjZGVmZ2hpamtsbW5vcA==";

    private static readonly PasswordOptions PasswordOptions = new()
    {
        Pepper = "test-pepper",
        Iterations = 120000,
        HashSize = 32
    };

    private static IPasswordService CreatePasswordService()
    {
        return new PasswordService(Options.Create(PasswordOptions));
    }

    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    [Test]
    public async Task Login_ReturnsUnauthorized_WhenUserDoesNotExist()
    {
        using var context = CreateInMemoryDbContext();
        var tokenServiceMock = new Mock<ITokenService>(MockBehavior.Strict);
        var passwordService = CreatePasswordService();
        var controller = new AuthController(context, tokenServiceMock.Object, passwordService);

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
        var passwordService = CreatePasswordService();
        var controller = new AuthController(context, tokenServiceMock.Object, passwordService);

        const string salt = SaltInvalidPassword;
        var admin = new Administrator
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            Salt = salt,
            PasswordHash = passwordService.HashPassword("correct-password", salt),
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
        var passwordService = CreatePasswordService();
        var controller = new AuthController(context, tokenServiceMock.Object, passwordService);

        const string salt = SaltInactive;
        var admin = new Administrator
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            Salt = salt,
            PasswordHash = passwordService.HashPassword("password", salt),
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
        var passwordService = CreatePasswordService();

        const string salt = SaltValid;
        var admin = new Administrator
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            Salt = salt,
            PasswordHash = passwordService.HashPassword("password", salt),
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

        var controller = new AuthController(context, tokenServiceMock.Object, passwordService);

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

    [Test]
    public async Task Login_ReturnsUnauthorized_WhenSaltFormatIsInvalid()
    {
        using var context = CreateInMemoryDbContext();
        var tokenServiceMock = new Mock<ITokenService>(MockBehavior.Strict);
        var passwordService = CreatePasswordService();
        var controller = new AuthController(context, tokenServiceMock.Object, passwordService);

        var admin = new Administrator
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            Salt = "invalid-salt",
            PasswordHash = "invalid-hash",
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
            Password = "password"
        };

        var result = await controller.Login(request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<UnauthorizedObjectResult>());
    }
}

