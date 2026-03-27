using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Meran.Back.Data;
using Meran.Back.DTO;

namespace Meran.Back.Controllers
{
    [ApiController]
    [Route("api")]
    public class MembershipsController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        public MembershipsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("applications/{applicationId:guid}/users/{userId:guid}/status")]
        [Authorize(Roles = "ApiClient")]
        public async Task<ActionResult<ApplicationUserStatusDto>> GetUserStatus(Guid applicationId, Guid userId, CancellationToken cancellationToken)
        {
            var user = await _dbContext.ApplicationUsers
                .AsNoTracking()
                .Include(x => x.Application)
                .Include(x => x.Plan)
                .SingleOrDefaultAsync(x => x.ApplicationId == applicationId && x.Id == userId, cancellationToken);

            if (user == null)
            {
                return NotFound();
            }

            var status = new ApplicationUserStatusDto
            {
                ApplicationId = user.ApplicationId,
                ApplicationUserId = user.Id,
                ApplicationPlanId = user.Plan?.Id ?? Guid.Empty,
                IsActive = user.IsActive,
                Plan = user.Plan,
                LastPaymentAt = user.LastPaymentAt,
                NextPaymentDueAt = user.NextPaymentDueAt,
                HasPaymentIssue = user.HasPaymentIssue
            };

            return Ok(status);
        }
    }
}

