using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Meran.Back.DTO;
using Meran.Back.Services;

namespace Meran.Back.Controllers
{
    [ApiController]
    [Route("api")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("applications/{applicationId:guid}/users/{userId:guid}/payments")]
        [Authorize(Roles = "ApiClient")]
        public async Task<ActionResult<PaymentEventDto>> AddPayment(Guid applicationId, Guid userId, CreatePaymentEventRequestDto request, CancellationToken cancellationToken)
        {
            var result = await _paymentService.AddPaymentAsync(applicationId, userId, request, cancellationToken);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpGet("payments/overview")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PaymentsOverviewDto>> GetOverview([FromQuery] Guid? applicationId, CancellationToken cancellationToken)
        {
            var result = await _paymentService.GetOverviewAsync(applicationId, cancellationToken);
            return Ok(result);
        }
    }
}

