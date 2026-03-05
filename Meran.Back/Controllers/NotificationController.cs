using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Meran.Back.DTO;
using Meran.Back.Services;

namespace Meran.Back.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpPost("payment-issues/run")]
        public async Task<ActionResult<List<ApplicationUserPaymentIssueDto>>> RunPaymentIssuesCheck(CancellationToken cancellationToken)
        {
            var issues = await _notificationService.CheckPaymentIssuesAsync(cancellationToken);
            return Ok(issues);
        }

        [HttpGet("payment-issues")]
        public async Task<ActionResult<List<ApplicationUserPaymentIssueDto>>> GetPaymentIssues(CancellationToken cancellationToken)
        {
            var issues = await _notificationService.GetCurrentPaymentIssuesAsync(cancellationToken);
            return Ok(issues);
        }
    }
}

