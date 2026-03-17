using Microsoft.EntityFrameworkCore;
using Meran.Back.Data;
using Meran.Back.DTO;
using Meran.Back.Models;
using Meran.Back.Services;

namespace Meran.Back.Tests.Services;

public class ApplicationServiceTest
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Test]
    public async Task CreateAsync_CreatesApplicationWithPlans()
    {
        using var context = CreateInMemoryDbContext();
        var service = new ApplicationService(context);

        var request = new CreateApplicationRequestDto
        {
            Name = "My app",
            Description = "Desc",
            Format = "subscription",
            OneShotPrice = null,
            Plans = new List<ApplicationPlanDto>
            {
                new ApplicationPlanDto
                {
                    Name = "Monthly",
                    Description = "Monthly plan",
                    BillingPeriod = "monthly",
                    Price = 10
                }
            }
        };

        var result = await service.CreateAsync(request, CancellationToken.None);

        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.Name, Is.EqualTo(request.Name));
        Assert.That(result.Plans, Has.Count.EqualTo(1));

        var appInDb = await context.Applications.Include(a => a.Plans).SingleAsync();
        Assert.That(appInDb.Name, Is.EqualTo(request.Name));
        Assert.That(appInDb.Plans.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task UpdateAsync_ReturnsNull_WhenApplicationDoesNotExist()
    {
        using var context = CreateInMemoryDbContext();
        var service = new ApplicationService(context);

        var request = new UpdateApplicationRequestDto
        {
            Name = "Updated",
            Description = "Updated",
            Format = "subscription",
            OneShotPrice = 10,
            Plans = new List<ApplicationPlanDto>()
        };

        var result = await service.UpdateAsync(Guid.NewGuid(), request, CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_UpdatesApplicationAndPlans()
    {
        using var context = CreateInMemoryDbContext();

        var app = new Application
        {
            Id = Guid.NewGuid(),
            Name = "Old",
            Description = "Old",
            Format = ApplicationFormat.Free,
            CreatedAt = DateTime.UtcNow
        };

        context.Applications.Add(app);
        await context.SaveChangesAsync();

        var service = new ApplicationService(context);

        var request = new UpdateApplicationRequestDto
        {
            Name = "New name",
            Description = "New desc",
            Format = "subscription",
            OneShotPrice = 20,
            Plans = new List<ApplicationPlanDto>
            {
                new ApplicationPlanDto
                {
                    Name = "New plan",
                    Description = "New",
                    BillingPeriod = "annual",
                    Price = 100
                }
            }
        };

        var result = await service.UpdateAsync(app.Id, request, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("New name"));
        Assert.That(result.Plans, Has.Count.EqualTo(1));
        Assert.That(result.Plans[0].Name, Is.EqualTo("New plan"));

        var appInDb = await context.Applications.Include(a => a.Plans).SingleAsync(a => a.Id == app.Id);
        Assert.That(appInDb.Plans.Count, Is.EqualTo(1));
        Assert.That(appInDb.Plans.Single().Name, Is.EqualTo("New plan"));
    }

    [Test]
    public async Task DeleteAsync_ReturnsFalse_WhenApplicationDoesNotExist()
    {
        using var context = CreateInMemoryDbContext();
        var service = new ApplicationService(context);

        var result = await service.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteAsync_ReturnsFalse_WhenApplicationHasUsers()
    {
        using var context = CreateInMemoryDbContext();

        var app = new Application
        {
            Id = Guid.NewGuid(),
            Name = "App",
            Description = "Desc",
            Format = ApplicationFormat.Free,
            CreatedAt = DateTime.UtcNow
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Name = "User",
            Email = "user@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow
        };

        app.Users.Add(user);

        context.Applications.Add(app);
        await context.SaveChangesAsync();

        var service = new ApplicationService(context);

        var result = await service.DeleteAsync(app.Id, CancellationToken.None);

        Assert.That(result, Is.False);
        Assert.That(await context.Applications.AnyAsync(a => a.Id == app.Id), Is.True);
    }

    [Test]
    public async Task DeleteAsync_RemovesApplication_WhenItHasNoUsers()
    {
        using var context = CreateInMemoryDbContext();

        var app = new Application
        {
            Id = Guid.NewGuid(),
            Name = "App",
            Description = "Desc",
            Format = ApplicationFormat.Free,
            CreatedAt = DateTime.UtcNow
        };

        context.Applications.Add(app);
        await context.SaveChangesAsync();

        var service = new ApplicationService(context);

        var result = await service.DeleteAsync(app.Id, CancellationToken.None);

        Assert.That(result, Is.True);
        Assert.That(await context.Applications.AnyAsync(a => a.Id == app.Id), Is.False);
    }

    [Test]
    public async Task AddUserAsync_ReturnsNull_WhenApplicationDoesNotExist()
    {
        using var context = CreateInMemoryDbContext();
        var service = new ApplicationService(context);

        var request = new AddApplicationUserRequestDto
        {
            Name = "User",
            Email = "user@example.com",
            Plan = "basic"
        };

        var result = await service.AddUserAsync(Guid.NewGuid(), request, CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AddUserAsync_Throws_WhenUserWithEmailAlreadyExists()
    {
        using var context = CreateInMemoryDbContext();

        var app = new Application
        {
            Id = Guid.NewGuid(),
            Name = "App",
            Description = "Desc",
            Format = ApplicationFormat.Free,
            CreatedAt = DateTime.UtcNow
        };

        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Name = "Existing",
            Email = "user@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow
        };

        context.Applications.Add(app);
        context.ApplicationUsers.Add(existingUser);
        await context.SaveChangesAsync();

        var service = new ApplicationService(context);

        var request = new AddApplicationUserRequestDto
        {
            Name = "User",
            Email = "user@example.com",
            Plan = "basic"
        };

        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.AddUserAsync(app.Id, request, CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("already exists"));
    }

    [Test]
    public async Task AddUserAsync_CreatesUser_WhenEmailIsUnique()
    {
        using var context = CreateInMemoryDbContext();

        var app = new Application
        {
            Id = Guid.NewGuid(),
            Name = "App",
            Description = "Desc",
            Format = ApplicationFormat.Free,
            CreatedAt = DateTime.UtcNow
        };

        context.Applications.Add(app);
        await context.SaveChangesAsync();

        var service = new ApplicationService(context);

        var request = new AddApplicationUserRequestDto
        {
            Name = "User",
            Email = "USER@Example.com",
            Plan = "basic"
        };

        var result = await service.AddUserAsync(app.Id, request, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Email, Is.EqualTo("user@example.com"));

        var usersInDb = await context.ApplicationUsers.Where(u => u.ApplicationId == app.Id).ToListAsync();
        Assert.That(usersInDb.Count, Is.EqualTo(1));
        Assert.That(usersInDb[0].Email, Is.EqualTo("user@example.com"));
    }
}

