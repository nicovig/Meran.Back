using Microsoft.EntityFrameworkCore;
using Meran.Back.Data;
using Meran.Back.DTO;
using Meran.Back.Models;
using Meran.Back.Services;

namespace Meran.Back.Tests.Services;

public class NotificationServiceTest
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Test]
    public async Task CheckPaymentIssuesAsync_DisablesUsersWithOverduePayments()
    {
        using var context = CreateInMemoryDbContext();

        var app = new Application
        {
            Id = Guid.NewGuid(),
            Name = "App",
            Description = "Desc",
            Format = ApplicationFormat.Subscription,
            CreatedAt = DateTime.UtcNow
        };

        var overdueUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Name = "Overdue",
            Email = "overdue@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            Plan = "pro",
            IsActive = true,
            NextPaymentDueAt = DateTime.UtcNow.AddDays(-1),
            HasPaymentIssue = false
        };

        var upToDateUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Name = "UpToDate",
            Email = "ok@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            Plan = "basic",
            IsActive = true,
            NextPaymentDueAt = DateTime.UtcNow.AddDays(10),
            HasPaymentIssue = false
        };

        context.Applications.Add(app);
        context.ApplicationUsers.Add(overdueUser);
        context.ApplicationUsers.Add(upToDateUser);
        await context.SaveChangesAsync();

        var service = new NotificationService(context);

        var result = await service.CheckPaymentIssuesAsync(CancellationToken.None);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].ApplicationUserId, Is.EqualTo(overdueUser.Id));

        var refreshedOverdue = await context.ApplicationUsers.SingleAsync(u => u.Id == overdueUser.Id);
        var refreshedUpToDate = await context.ApplicationUsers.SingleAsync(u => u.Id == upToDateUser.Id);

        Assert.That(refreshedOverdue.IsActive, Is.False);
        Assert.That(refreshedOverdue.HasPaymentIssue, Is.True);
        Assert.That(refreshedUpToDate.IsActive, Is.True);
        Assert.That(refreshedUpToDate.HasPaymentIssue, Is.False);
    }

    [Test]
    public async Task CheckPaymentIssuesAsync_DoesNothing_WhenNoOverduePayments()
    {
        using var context = CreateInMemoryDbContext();

        var app = new Application
        {
            Id = Guid.NewGuid(),
            Name = "App",
            Description = "Desc",
            Format = ApplicationFormat.Subscription,
            CreatedAt = DateTime.UtcNow
        };

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Name = "User",
            Email = "user@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            Plan = "pro",
            IsActive = true,
            NextPaymentDueAt = DateTime.UtcNow.AddDays(5),
            HasPaymentIssue = false
        };

        context.Applications.Add(app);
        context.ApplicationUsers.Add(user);
        await context.SaveChangesAsync();

        var service = new NotificationService(context);

        var result = await service.CheckPaymentIssuesAsync(CancellationToken.None);

        Assert.That(result, Is.Empty);

        var refreshedUser = await context.ApplicationUsers.SingleAsync(u => u.Id == user.Id);
        Assert.That(refreshedUser.IsActive, Is.True);
        Assert.That(refreshedUser.HasPaymentIssue, Is.False);
    }

    [Test]
    public async Task GetCurrentPaymentIssuesAsync_ReturnsUsersWithPaymentIssues()
    {
        using var context = CreateInMemoryDbContext();

        var app = new Application
        {
            Id = Guid.NewGuid(),
            Name = "App",
            Description = "Desc",
            Format = ApplicationFormat.Subscription,
            CreatedAt = DateTime.UtcNow
        };

        var userWithIssue = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Name = "Issue",
            Email = "issue@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            Plan = "pro",
            IsActive = false,
            HasPaymentIssue = true
        };

        var userWithoutIssue = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Name = "Ok",
            Email = "ok@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            Plan = "basic",
            IsActive = true,
            HasPaymentIssue = false
        };

        context.Applications.Add(app);
        context.ApplicationUsers.Add(userWithIssue);
        context.ApplicationUsers.Add(userWithoutIssue);
        await context.SaveChangesAsync();

        var service = new NotificationService(context);

        var result = await service.GetCurrentPaymentIssuesAsync(CancellationToken.None);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].ApplicationUserId, Is.EqualTo(userWithIssue.Id));
        Assert.That(result[0].ApplicationName, Is.EqualTo(app.Name));
    }
}

