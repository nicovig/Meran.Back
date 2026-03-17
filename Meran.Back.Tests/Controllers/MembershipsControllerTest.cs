using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Meran.Back.Controllers;
using Meran.Back.Data;
using Meran.Back.Models;

namespace Meran.Back.Tests.Controllers;

public class MembershipsControllerTest
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static MembershipsController CreateController(ApplicationDbContext context, bool authenticatedApiClient = true)
    {
        var controller = new MembershipsController(context);

        var httpContext = new DefaultHttpContext();
        if (authenticatedApiClient)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "ApiClient"),
                new Claim(ClaimTypes.NameIdentifier, "test-client")
            }, "TestAuth");

            httpContext.User = new ClaimsPrincipal(identity);
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    [Test]
    public async Task GetUserStatus_ReturnsNotFound_WhenUserDoesNotExist()
    {
        using var context = CreateInMemoryDbContext();
        var controller = CreateController(context);

        var result = await controller.GetUserStatus(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
    }

    [Test]
    public async Task GetUserStatus_ReturnsStatus_WhenUserExists()
    {
        using var context = CreateInMemoryDbContext();

        var application = new Application
        {
            Id = Guid.NewGuid(),
            Name = "Test app",
            Description = "Test description",
            Format = ApplicationFormat.Subscription,
            CreatedAt = DateTime.UtcNow
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = application.Id,
            Name = "John Doe",
            Email = "john@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            Plan = "pro",
            IsActive = true,
            LastPaymentAt = DateTime.UtcNow.AddDays(-10),
            NextPaymentDueAt = DateTime.UtcNow.AddDays(20),
            HasPaymentIssue = false
        };

        context.Applications.Add(application);
        context.ApplicationUsers.Add(user);
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetUserStatus(application.Id, user.Id, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.Not.Null);
    }
}
