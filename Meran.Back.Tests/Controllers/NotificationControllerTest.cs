using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Meran.Back.Controllers;
using Meran.Back.DTO;
using Meran.Back.Services;
using Moq;

namespace Meran.Back.Tests.Controllers;

public class NotificationControllerTest
{
    private static (NotificationController controller, Mock<INotificationService> serviceMock) CreateController()
    {
        var serviceMock = new Mock<INotificationService>(MockBehavior.Strict);
        var controller = new NotificationController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return (controller, serviceMock);
    }

    [Test]
    public async Task RunPaymentIssuesCheck_ReturnsOkWithIssues()
    {
        var (controller, serviceMock) = CreateController();

        var issues = new List<ApplicationUserPaymentIssueDto>
        {
            new ApplicationUserPaymentIssueDto
            {
                ApplicationId = Guid.NewGuid(),
                ApplicationUserId = Guid.NewGuid(),
                ApplicationName = "App1",
                UserName = "User1",
                UserEmail = "user1@example.com",
                Plan = "basic",
                LastPaymentAt = DateTime.UtcNow.AddDays(-40),
                NextPaymentDueAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        serviceMock
            .Setup(s => s.CheckPaymentIssuesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(issues);

        var result = await controller.RunPaymentIssuesCheck(CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.EqualTo(issues));
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task GetPaymentIssues_ReturnsOkWithIssues()
    {
        var (controller, serviceMock) = CreateController();

        var issues = new List<ApplicationUserPaymentIssueDto>
        {
            new ApplicationUserPaymentIssueDto
            {
                ApplicationId = Guid.NewGuid(),
                ApplicationUserId = Guid.NewGuid(),
                ApplicationName = "App1",
                UserName = "User1",
                UserEmail = "user1@example.com",
                Plan = "basic",
                LastPaymentAt = DateTime.UtcNow.AddDays(-40),
                NextPaymentDueAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        serviceMock
            .Setup(s => s.GetCurrentPaymentIssuesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(issues);

        var result = await controller.GetPaymentIssues(CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.EqualTo(issues));
        serviceMock.VerifyAll();
    }
}

