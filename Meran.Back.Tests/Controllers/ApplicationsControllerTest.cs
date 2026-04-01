using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Meran.Back.Controllers;
using Meran.Back.DTO;
using Meran.Back.Services;
using Moq;

namespace Meran.Back.Tests.Controllers;

public class ApplicationsControllerTest
{
    private static (ApplicationsController controller, Mock<IApplicationService> serviceMock) CreateController()
    {
        var serviceMock = new Mock<IApplicationService>(MockBehavior.Strict);
        var controller = new ApplicationsController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        return (controller, serviceMock);
    }

    [Test]
    public async Task GetAll_ReturnsOkWithApplications()
    {
        var (controller, serviceMock) = CreateController();
        var apps = new List<ApplicationDto>
        {
            new ApplicationDto { Id = Guid.NewGuid(), Name = "App1", Description = "Desc1", Format = "free", CreatedAt = DateTime.UtcNow },
            new ApplicationDto { Id = Guid.NewGuid(), Name = "App2", Description = "Desc2", Format = "subscription", CreatedAt = DateTime.UtcNow }
        };

        serviceMock
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(apps);

        var result = await controller.GetAll(CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.EqualTo(apps));
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task Create_ReturnsCreatedAtAction_WithCreatedApplication()
    {
        var (controller, serviceMock) = CreateController();

        var request = new CreateApplicationRequestDto
        {
            Name = "New app",
            Description = "New desc",
            Format = "subscription",
            OneShotPrice = null
        };

        var created = new ApplicationDto
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Format = request.Format,
            CreatedAt = DateTime.UtcNow
        };

        serviceMock
            .Setup(s => s.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var result = await controller.Create(request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<CreatedAtActionResult>());
        var createdResult = (CreatedAtActionResult)result.Result!;
        Assert.That(createdResult.Value, Is.EqualTo(created));
        Assert.That(createdResult.ActionName, Is.EqualTo(nameof(ApplicationsController.GetAll)));
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task Update_ReturnsNotFound_WhenApplicationDoesNotExist()
    {
        var (controller, serviceMock) = CreateController();
        var id = Guid.NewGuid();
        var request = new UpdateApplicationRequestDto
        {
            Name = "Updated",
            Description = "Updated",
            Format = "subscription",
            OneShotPrice = 10
        };

        serviceMock
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationDto?)null);

        var result = await controller.Update(id, request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task Update_ReturnsOk_WhenApplicationIsUpdated()
    {
        var (controller, serviceMock) = CreateController();
        var id = Guid.NewGuid();
        var request = new UpdateApplicationRequestDto
        {
            Name = "Updated",
            Description = "Updated",
            Format = "subscription",
            OneShotPrice = 10
        };

        var updated = new ApplicationDto
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            Format = request.Format,
            CreatedAt = DateTime.UtcNow
        };

        serviceMock
            .Setup(s => s.UpdateAsync(id, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var result = await controller.Update(id, request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.EqualTo(updated));
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task Delete_ReturnsNoContent_WhenDeleteSucceeds()
    {
        var (controller, serviceMock) = CreateController();
        var id = Guid.NewGuid();

        serviceMock
            .Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await controller.Delete(id, CancellationToken.None);

        Assert.That(result, Is.TypeOf<NoContentResult>());
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task Delete_ReturnsConflict_WhenDeleteFails()
    {
        var (controller, serviceMock) = CreateController();
        var id = Guid.NewGuid();

        serviceMock
            .Setup(s => s.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await controller.Delete(id, CancellationToken.None);

        Assert.That(result, Is.TypeOf<ConflictObjectResult>());
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task AddUser_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var (controller, serviceMock) = CreateController();
        var applicationId = Guid.NewGuid();
        var request = new AddApplicationUserRequestDto
        {
            Name = "User",
            Email = "user@example.com",
            Plan = "basic"
        };

        serviceMock
            .Setup(s => s.AddUserAsync(applicationId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUserDto?)null);

        var result = await controller.AddUser(applicationId, request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task AddUser_ReturnsOk_WhenUserIsCreated()
    {
        var (controller, serviceMock) = CreateController();
        var applicationId = Guid.NewGuid();
        var request = new AddApplicationUserRequestDto
        {
            Name = "User",
            Email = "user@example.com",
            Plan = "basic"
        };

        var userDto = new ApplicationUserDto
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            Name = request.Name,
            Email = request.Email,
            Origin = "admin",
            CreatedAt = DateTime.UtcNow,
            Plan = request.Plan
        };

        serviceMock
            .Setup(s => s.AddUserAsync(applicationId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);

        var result = await controller.AddUser(applicationId, request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.EqualTo(userDto));
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task AddUser_ReturnsConflict_WhenServiceThrowsInvalidOperation()
    {
        var (controller, serviceMock) = CreateController();
        var applicationId = Guid.NewGuid();
        var request = new AddApplicationUserRequestDto
        {
            Name = "User",
            Email = "user@example.com",
            Plan = "basic"
        };

        serviceMock
            .Setup(s => s.AddUserAsync(applicationId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("User with this email already exists for this application."));

        var result = await controller.AddUser(applicationId, request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ConflictObjectResult>());
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task GetFeatures_ReturnsOk_WhenFeaturesExist()
    {
        var (controller, serviceMock) = CreateController();
        var applicationId = Guid.NewGuid();
        var features = new List<ApplicationFeatureDto>
        {
            new ApplicationFeatureDto
            {
                Id = Guid.NewGuid(),
                Key = "maxProjects",
                Type = "integer",
                PlanValues = new List<ApplicationFeaturePlanValueDto>
                {
                    new ApplicationFeaturePlanValueDto { ApplicationPlanId = Guid.NewGuid(), Value = "10" }
                }
            }
        };

        serviceMock
            .Setup(s => s.GetFeaturesAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(features);

        var result = await controller.GetFeatures(applicationId, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.EqualTo(features));
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task UpsertFeatures_ReturnsOk_WhenServiceSucceeds()
    {
        var (controller, serviceMock) = CreateController();
        var applicationId = Guid.NewGuid();
        var request = new UpsertApplicationFeaturesRequestDto
        {
            Features = new List<UpsertApplicationFeatureRequestDto>
            {
                new UpsertApplicationFeatureRequestDto
                {
                    Key = "maxProjects",
                    Type = "integer",
                    PlanValues = new List<ApplicationFeaturePlanValueDto>
                    {
                        new ApplicationFeaturePlanValueDto { ApplicationPlanId = Guid.NewGuid(), Value = "10" }
                    }
                }
            }
        };

        var features = new List<ApplicationFeatureDto>
        {
            new ApplicationFeatureDto
            {
                Id = Guid.NewGuid(),
                Key = "maxProjects",
                Type = "integer"
            }
        };

        serviceMock
            .Setup(s => s.UpsertFeaturesAsync(applicationId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(features);

        var result = await controller.UpsertFeatures(applicationId, request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task GetSubscriptions_ReturnsOk_WhenSubscriptionsExist()
    {
        var (controller, serviceMock) = CreateController();
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var subscriptions = new List<SubscriptionDto>
        {
            new SubscriptionDto
            {
                Id = Guid.NewGuid(),
                ApplicationUserId = userId,
                ApplicationPlanId = Guid.NewGuid(),
                Status = "active",
                StartedAt = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
            }
        };

        serviceMock
            .Setup(s => s.GetSubscriptionsAsync(applicationId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriptions);

        var result = await controller.GetSubscriptions(applicationId, userId, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task CreateSubscription_ReturnsOk_WhenServiceSucceeds()
    {
        var (controller, serviceMock) = CreateController();
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreateSubscriptionRequestDto
        {
            ApplicationPlanId = Guid.NewGuid(),
            Status = "active",
            StartedAt = DateTime.UtcNow,
            CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
        };

        var subscription = new SubscriptionDto
        {
            Id = Guid.NewGuid(),
            ApplicationUserId = userId,
            ApplicationPlanId = request.ApplicationPlanId,
            Status = "active",
            StartedAt = request.StartedAt,
            CurrentPeriodEnd = request.CurrentPeriodEnd
        };

        serviceMock
            .Setup(s => s.CreateSubscriptionAsync(applicationId, userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscription);

        var result = await controller.CreateSubscription(applicationId, userId, request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        serviceMock.VerifyAll();
    }
}

