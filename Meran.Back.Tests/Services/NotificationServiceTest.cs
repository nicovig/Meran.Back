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
            CreatedAt = DateTime.UtcNow,
            Plans = new List<ApplicationPlan>
            {
                new ApplicationPlan
                {
                    Id = Guid.NewGuid(),
                    Name = "Pro",
                    Description = "Pro",
                    BillingPeriod = BillingPeriod.Monthly,
                    Price = 20
                }
            }
        };

        var plan = app.Plans.Single();

        var overdueUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Name = "Overdue",
            Email = "overdue@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var upToDateUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Name = "UpToDate",
            Email = "ok@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var overdueSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = overdueUser.Id,
            ApplicationPlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            StartedAt = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(-1)
        };

        var upToDateSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = upToDateUser.Id,
            ApplicationPlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            StartedAt = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(10)
        };

        context.Applications.Add(app);
        context.ApplicationUsers.Add(overdueUser);
        context.ApplicationUsers.Add(upToDateUser);
        context.Subscriptions.Add(overdueSubscription);
        context.Subscriptions.Add(upToDateSubscription);
        await context.SaveChangesAsync();

        var service = new NotificationService(context);

        var result = await service.CheckPaymentIssuesAsync(CancellationToken.None);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].ApplicationUserId, Is.EqualTo(overdueUser.Id));

        var refreshedOverdue = await context.ApplicationUsers.SingleAsync(u => u.Id == overdueUser.Id);
        var refreshedUpToDate = await context.ApplicationUsers.SingleAsync(u => u.Id == upToDateUser.Id);
        var refreshedOverdueSubscription = await context.Subscriptions.SingleAsync(x => x.Id == overdueSubscription.Id);
        var refreshedUpToDateSubscription = await context.Subscriptions.SingleAsync(x => x.Id == upToDateSubscription.Id);

        Assert.That(refreshedOverdue.IsActive, Is.False);
        Assert.That(refreshedUpToDate.IsActive, Is.True);
        Assert.That(refreshedOverdueSubscription.Status, Is.EqualTo(SubscriptionStatus.PastDue));
        Assert.That(refreshedUpToDateSubscription.Status, Is.EqualTo(SubscriptionStatus.Active));
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
            CreatedAt = DateTime.UtcNow,
            Plans = new List<ApplicationPlan>
            {
                new ApplicationPlan
                {
                    Id = Guid.NewGuid(),
                    Name = "Pro",
                    Description = "Pro",
                    BillingPeriod = BillingPeriod.Monthly,
                    Price = 20
                }
            }
        };

        var plan = app.Plans.Single();

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Name = "User",
            Email = "user@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = user.Id,
            ApplicationPlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            StartedAt = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(5)
        };

        context.Applications.Add(app);
        context.ApplicationUsers.Add(user);
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();

        var service = new NotificationService(context);

        var result = await service.CheckPaymentIssuesAsync(CancellationToken.None);

        Assert.That(result, Is.Empty);

        var refreshedUser = await context.ApplicationUsers.SingleAsync(u => u.Id == user.Id);
        var refreshedSubscription = await context.Subscriptions.SingleAsync(x => x.Id == subscription.Id);
        Assert.That(refreshedUser.IsActive, Is.True);
        Assert.That(refreshedSubscription.Status, Is.EqualTo(SubscriptionStatus.Active));
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
            CreatedAt = DateTime.UtcNow,
            Plans = new List<ApplicationPlan>
            {
                new ApplicationPlan
                {
                    Id = Guid.NewGuid(),
                    Name = "Pro",
                    Description = "Pro",
                    BillingPeriod = BillingPeriod.Monthly,
                    Price = 20
                }
            }
        };

        var plan = app.Plans.Single();

        var userWithIssue = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Name = "Issue",
            Email = "issue@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            IsActive = false
        };

        var userWithoutIssue = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Name = "Ok",
            Email = "ok@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var issueSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = userWithIssue.Id,
            ApplicationPlanId = plan.Id,
            Status = SubscriptionStatus.PastDue,
            StartedAt = DateTime.UtcNow.AddMonths(-2),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(-2)
        };

        var okSubscription = new Subscription
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = userWithoutIssue.Id,
            ApplicationPlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            StartedAt = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(10)
        };

        context.Applications.Add(app);
        context.ApplicationUsers.Add(userWithIssue);
        context.ApplicationUsers.Add(userWithoutIssue);
        context.Subscriptions.Add(issueSubscription);
        context.Subscriptions.Add(okSubscription);
        await context.SaveChangesAsync();

        var service = new NotificationService(context);

        var result = await service.GetCurrentPaymentIssuesAsync(CancellationToken.None);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].ApplicationUserId, Is.EqualTo(userWithIssue.Id));
        Assert.That(result[0].ApplicationName, Is.EqualTo(app.Name));
    }
}

