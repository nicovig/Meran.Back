using Microsoft.EntityFrameworkCore;
using Meran.Back.Data;
using Meran.Back.DTO;
using Meran.Back.Models;
using Meran.Back.Services;

namespace Meran.Back.Tests.Services;

public class PaymentServiceTest
{
    private static ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Test]
    public async Task AddPaymentAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        using var context = CreateInMemoryDbContext();
        var service = new PaymentService(context);

        var request = new CreatePaymentEventRequestDto
        {
            Type = "initial",
            Amount = 10,
            Currency = "EUR",
            OccurredAt = DateTime.UtcNow
        };

        var result = await service.AddPaymentAsync(Guid.NewGuid(), Guid.NewGuid(), request, CancellationToken.None);

        Assert.That(result, Is.Null);
        Assert.That(await context.PaymentEvents.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task AddPaymentAsync_InitialPayment_ActivatesUserAndClearsIssue()
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
            IsActive = false
        };

        var plan = new ApplicationPlan
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Name = "Pro",
            Description = "Pro",
            BillingPeriod = BillingPeriod.Monthly,
            Price = 20
        };

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = user.Id,
            ApplicationPlanId = plan.Id,
            Status = SubscriptionStatus.PastDue,
            StartedAt = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(-1)
        };

        context.Applications.Add(app);
        context.ApplicationPlans.Add(plan);
        context.ApplicationUsers.Add(user);
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();

        var service = new PaymentService(context);

        var now = DateTime.UtcNow;
        var nextDue = now.AddMonths(1);

        var request = new CreatePaymentEventRequestDto
        {
            Type = "initial",
            Amount = 20,
            Currency = "EUR",
            OccurredAt = now,
            Provider = "stripe",
            ProviderReference = "pay_123",
            NextPaymentDueAt = nextDue
        };

        var result = await service.AddPaymentAsync(app.Id, user.Id, request, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ApplicationUserId, Is.EqualTo(user.Id));
        Assert.That(result.Type, Is.EqualTo("paymentSucceeded"));

        var refreshedUser = await context.ApplicationUsers.SingleAsync(u => u.Id == user.Id);
        var refreshedSubscription = await context.Subscriptions.SingleAsync(x => x.Id == subscription.Id);

        Assert.That(refreshedUser.IsActive, Is.True);
        Assert.That(refreshedSubscription.Status, Is.EqualTo(SubscriptionStatus.Active));
        Assert.That(refreshedSubscription.CurrentPeriodEnd, Is.EqualTo(nextDue));
    }

    [Test]
    public async Task AddPaymentAsync_FailedPayment_SetsPaymentIssue()
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
            IsActive = true
        };

        var plan = new ApplicationPlan
        {
            Id = Guid.NewGuid(),
            ApplicationId = app.Id,
            Name = "Pro",
            Description = "Pro",
            BillingPeriod = BillingPeriod.Monthly,
            Price = 20
        };

        var subscription = new Subscription
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = user.Id,
            ApplicationPlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            StartedAt = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(10)
        };

        context.Applications.Add(app);
        context.ApplicationPlans.Add(plan);
        context.ApplicationUsers.Add(user);
        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();

        var service = new PaymentService(context);

        var request = new CreatePaymentEventRequestDto
        {
            Type = "failed",
            Amount = 20,
            Currency = "EUR",
            OccurredAt = DateTime.UtcNow,
            Provider = "stripe",
            ProviderReference = "pay_failed"
        };

        var result = await service.AddPaymentAsync(app.Id, user.Id, request, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Type, Is.EqualTo("paymentFailed"));

        var refreshedSubscription = await context.Subscriptions.SingleAsync(x => x.Id == subscription.Id);

        Assert.That(refreshedSubscription.Status, Is.EqualTo(SubscriptionStatus.PastDue));
    }

    [Test]
    public async Task GetOverviewAsync_ReturnsPaymentsAndUpcomingForAllApplications()
    {
        using var context = CreateInMemoryDbContext();

        var app1 = new Application
        {
            Id = Guid.NewGuid(),
            Name = "App1",
            Description = "Desc1",
            Format = ApplicationFormat.Subscription,
            CreatedAt = DateTime.UtcNow
        };

        var app2 = new Application
        {
            Id = Guid.NewGuid(),
            Name = "App2",
            Description = "Desc2",
            Format = ApplicationFormat.Subscription,
            CreatedAt = DateTime.UtcNow
        };

        var user1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app1.Id,
            Name = "User1",
            Email = "user1@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var user2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app2.Id,
            Name = "User2",
            Email = "user2@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            IsActive = false
        };

        var plan1 = new ApplicationPlan
        {
            Id = Guid.NewGuid(),
            ApplicationId = app1.Id,
            Name = "Pro",
            Description = "Pro",
            BillingPeriod = BillingPeriod.Monthly,
            Price = 30
        };

        var plan2 = new ApplicationPlan
        {
            Id = Guid.NewGuid(),
            ApplicationId = app2.Id,
            Name = "Basic",
            Description = "Basic",
            BillingPeriod = BillingPeriod.Monthly,
            Price = 15
        };

        var sub1 = new Subscription
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = user1.Id,
            ApplicationPlanId = plan1.Id,
            Status = SubscriptionStatus.Active,
            StartedAt = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(10)
        };

        var sub2 = new Subscription
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = user2.Id,
            ApplicationPlanId = plan2.Id,
            Status = SubscriptionStatus.Canceled,
            StartedAt = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(5)
        };

        var evt1 = new PaymentEvent
        {
            Id = Guid.NewGuid(),
            ApplicationId = app1.Id,
            SubscriptionId = sub1.Id,
            EventType = PaymentEventType.PaymentSucceeded,
            Status = "succeeded",
            Amount = 30,
            Currency = "EUR",
            OccurredAt = DateTime.UtcNow.AddDays(-1)
        };

        var evt2 = new PaymentEvent
        {
            Id = Guid.NewGuid(),
            ApplicationId = app2.Id,
            SubscriptionId = sub2.Id,
            EventType = PaymentEventType.PaymentSucceeded,
            Status = "succeeded",
            Amount = 15,
            Currency = "USD",
            OccurredAt = DateTime.UtcNow.AddDays(-2)
        };

        context.Applications.AddRange(app1, app2);
        context.ApplicationPlans.AddRange(plan1, plan2);
        context.ApplicationUsers.AddRange(user1, user2);
        context.Subscriptions.AddRange(sub1, sub2);
        context.PaymentEvents.AddRange(evt1, evt2);
        await context.SaveChangesAsync();

        var service = new PaymentService(context);

        var overview = await service.GetOverviewAsync(null, CancellationToken.None);

        Assert.That(overview.PastPayments, Has.Count.EqualTo(2));
        Assert.That(overview.PastPayments[0].OccurredAt, Is.GreaterThanOrEqualTo(overview.PastPayments[1].OccurredAt));

        Assert.That(overview.UpcomingPayments, Has.Count.EqualTo(1));
        Assert.That(overview.UpcomingPayments[0].ApplicationUserId, Is.EqualTo(user1.Id));
        Assert.That(overview.UpcomingPayments[0].ExpectedAmount, Is.EqualTo(30));
        Assert.That(overview.UpcomingPayments[0].Currency, Is.EqualTo("EUR"));
    }

    [Test]
    public async Task GetOverviewAsync_FiltersByApplicationId()
    {
        using var context = CreateInMemoryDbContext();

        var app1 = new Application
        {
            Id = Guid.NewGuid(),
            Name = "App1",
            Description = "Desc1",
            Format = ApplicationFormat.Subscription,
            CreatedAt = DateTime.UtcNow
        };

        var app2 = new Application
        {
            Id = Guid.NewGuid(),
            Name = "App2",
            Description = "Desc2",
            Format = ApplicationFormat.Subscription,
            CreatedAt = DateTime.UtcNow
        };

        var user1 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app1.Id,
            Name = "User1",
            Email = "user1@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var user2 = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            ApplicationId = app2.Id,
            Name = "User2",
            Email = "user2@example.com",
            Origin = UserOrigin.Admin,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var plan1 = new ApplicationPlan
        {
            Id = Guid.NewGuid(),
            ApplicationId = app1.Id,
            Name = "Pro",
            Description = "Pro",
            BillingPeriod = BillingPeriod.Monthly,
            Price = 30
        };

        var plan2 = new ApplicationPlan
        {
            Id = Guid.NewGuid(),
            ApplicationId = app2.Id,
            Name = "Basic",
            Description = "Basic",
            BillingPeriod = BillingPeriod.Monthly,
            Price = 15
        };

        var sub1 = new Subscription
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = user1.Id,
            ApplicationPlanId = plan1.Id,
            Status = SubscriptionStatus.Active,
            StartedAt = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(10)
        };

        var sub2 = new Subscription
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = user2.Id,
            ApplicationPlanId = plan2.Id,
            Status = SubscriptionStatus.Active,
            StartedAt = DateTime.UtcNow.AddMonths(-1),
            CurrentPeriodEnd = DateTime.UtcNow.AddDays(5)
        };

        var evt1 = new PaymentEvent
        {
            Id = Guid.NewGuid(),
            ApplicationId = app1.Id,
            SubscriptionId = sub1.Id,
            EventType = PaymentEventType.PaymentSucceeded,
            Status = "succeeded",
            Amount = 30,
            Currency = "EUR",
            OccurredAt = DateTime.UtcNow.AddDays(-1)
        };

        var evt2 = new PaymentEvent
        {
            Id = Guid.NewGuid(),
            ApplicationId = app2.Id,
            SubscriptionId = sub2.Id,
            EventType = PaymentEventType.PaymentSucceeded,
            Status = "succeeded",
            Amount = 15,
            Currency = "USD",
            OccurredAt = DateTime.UtcNow.AddDays(-2)
        };

        context.Applications.AddRange(app1, app2);
        context.ApplicationPlans.AddRange(plan1, plan2);
        context.ApplicationUsers.AddRange(user1, user2);
        context.Subscriptions.AddRange(sub1, sub2);
        context.PaymentEvents.AddRange(evt1, evt2);
        await context.SaveChangesAsync();

        var service = new PaymentService(context);

        var overview = await service.GetOverviewAsync(app1.Id, CancellationToken.None);

        Assert.That(overview.PastPayments, Has.Count.EqualTo(1));
        Assert.That(overview.PastPayments[0].ApplicationId, Is.EqualTo(app1.Id));

        Assert.That(overview.UpcomingPayments, Has.Count.EqualTo(1));
        Assert.That(overview.UpcomingPayments[0].ApplicationUserId, Is.EqualTo(user1.Id));
    }
}

