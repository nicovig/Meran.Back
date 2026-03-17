using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Meran.Back.Controllers;
using Meran.Back.DTO;
using Meran.Back.Services;
using Moq;

namespace Meran.Back.Tests.Controllers;

public class PaymentsControllerTest
{
    private static (PaymentsController controller, Mock<IPaymentService> serviceMock) CreateController()
    {
        var serviceMock = new Mock<IPaymentService>(MockBehavior.Strict);
        var controller = new PaymentsController(serviceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return (controller, serviceMock);
    }

    [Test]
    public async Task AddPayment_ReturnsNotFound_WhenServiceReturnsNull()
    {
        var (controller, serviceMock) = CreateController();
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreatePaymentEventRequestDto
        {
            Type = "charge.succeeded",
            Amount = 100,
            Currency = "EUR",
            OccurredAt = DateTime.UtcNow
        };

        serviceMock
            .Setup(s => s.AddPaymentAsync(applicationId, userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentEventDto?)null);

        var result = await controller.AddPayment(applicationId, userId, request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task AddPayment_ReturnsOk_WhenPaymentIsCreated()
    {
        var (controller, serviceMock) = CreateController();
        var applicationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var request = new CreatePaymentEventRequestDto
        {
            Type = "charge.succeeded",
            Amount = 100,
            Currency = "EUR",
            OccurredAt = DateTime.UtcNow
        };

        var payment = new PaymentEventDto
        {
            Id = Guid.NewGuid(),
            ApplicationId = applicationId,
            ApplicationUserId = userId,
            Type = request.Type,
            Amount = request.Amount,
            Currency = request.Currency,
            OccurredAt = request.OccurredAt
        };

        serviceMock
            .Setup(s => s.AddPaymentAsync(applicationId, userId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        var result = await controller.AddPayment(applicationId, userId, request, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.EqualTo(payment));
        serviceMock.VerifyAll();
    }

    [Test]
    public async Task GetOverview_ReturnsOk_WithOverview()
    {
        var (controller, serviceMock) = CreateController();
        Guid? applicationId = Guid.NewGuid();

        var overview = new PaymentsOverviewDto
        {
            PastPayments = new List<PaymentEventDto>
            {
                new PaymentEventDto
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = applicationId.Value,
                    ApplicationUserId = Guid.NewGuid(),
                    Type = "charge.succeeded",
                    Amount = 100,
                    Currency = "EUR",
                    OccurredAt = DateTime.UtcNow.AddDays(-1)
                }
            },
            UpcomingPayments = new List<ScheduledPaymentDto>()
        };

        serviceMock
            .Setup(s => s.GetOverviewAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(overview);

        var result = await controller.GetOverview(applicationId, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.EqualTo(overview));
        serviceMock.VerifyAll();
    }
}

