using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Meran.Back.Controllers;
using Meran.Back.Data;
using Meran.Back.DTO;
using Meran.Back.Models;
using Meran.Back.Services;
using Moq;

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

    private static IOptions<MachineClientOptions> EmptyMachineClientOptions()
    {
        return Options.Create(new MachineClientOptions());
    }

    [Test]
    public async Task Login_ReturnsUnauthorized_WhenUserDoesNotExist()
    {
        using var context = CreateInMemoryDbContext();
        var tokenServiceMock = new Mock<ITokenService>(MockBehavior.Strict);
        var passwordService = CreatePasswordService();
        var controller = new AuthController(context, tokenServiceMock.Object, passwordService, EmptyMachineClientOptions());

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
        var controller = new AuthController(context, tokenServiceMock.Object, passwordService, EmptyMachineClientOptions());

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
        var controller = new AuthController(context, tokenServiceMock.Object, passwordService, EmptyMachineClientOptions());

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

        var controller = new AuthController(context, tokenServiceMock.Object, passwordService, EmptyMachineClientOptions());

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
        var controller = new AuthController(context, tokenServiceMock.Object, passwordService, EmptyMachineClientOptions());

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

    [Test]
    public async Task Token_Returns503_WhenMachineClientSecretNotConfigured()
    {
        using var context = CreateInMemoryDbContext();
        var tokenServiceMock = new Mock<ITokenService>(MockBehavior.Strict);
        var passwordService = CreatePasswordService();
        var controller = new AuthController(context, tokenServiceMock.Object, passwordService, EmptyMachineClientOptions())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Token(CancellationToken.None);

        Assert.That(result, Is.TypeOf<ObjectResult>());
        var obj = (ObjectResult)result;
        Assert.That(obj.StatusCode, Is.EqualTo(503));
    }

    [Test]
    public async Task Token_ReturnsUnauthorized_WhenInvalidClient()
    {
        using var context = CreateInMemoryDbContext();
        var tokenServiceMock = new Mock<ITokenService>(MockBehavior.Strict);
        var passwordService = CreatePasswordService();
        var options = Options.Create(new MachineClientOptions
        {
            ClientId = "kalon-backend",
            ClientSecret = "correct-secret",
            Role = "ApiClient",
            AccessTokenExpiresMinutes = 60
        });
        var controller = new AuthController(context, tokenServiceMock.Object, passwordService, options);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "application/json";
        var body = """{"grant_type":"client_credentials","client_id":"kalon-backend","client_secret":"wrong"}""";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Token(CancellationToken.None);

        Assert.That(result, Is.TypeOf<UnauthorizedObjectResult>());
    }

    [Test]
    public async Task Token_ReturnsOk_WhenCredentialsValid()
    {
        using var context = CreateInMemoryDbContext();
        var tokenServiceMock = new Mock<ITokenService>(MockBehavior.Strict);
        var passwordService = CreatePasswordService();
        var options = Options.Create(new MachineClientOptions
        {
            ClientId = "kalon-backend",
            ClientSecret = "correct-secret",
            Role = "ApiClient",
            AccessTokenExpiresMinutes = 60
        });
        tokenServiceMock
            .Setup(s => s.GenerateMachineAccessToken("kalon-backend", "ApiClient", 60))
            .Returns("m2m-token");

        var controller = new AuthController(context, tokenServiceMock.Object, passwordService, options);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "application/json";
        var body = """{"grant_type":"client_credentials","client_id":"kalon-backend","client_secret":"correct-secret"}""";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Token(CancellationToken.None);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        var dto = (ClientCredentialsTokenResponseDto)ok.Value!;
        Assert.That(dto.access_token, Is.EqualTo("m2m-token"));
        Assert.That(dto.expires_in, Is.EqualTo(3600));
        tokenServiceMock.VerifyAll();
    }
}

