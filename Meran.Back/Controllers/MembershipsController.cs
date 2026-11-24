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
                .Include(x => x.Subscriptions)
                    .ThenInclude(x => x.ApplicationPlan)
                        .ThenInclude(p => p.FeatureValues)
                            .ThenInclude(fv => fv.ApplicationFeature)
                .SingleOrDefaultAsync(x => x.ApplicationId == applicationId && x.Id == userId, cancellationToken);

            if (user == null)
            {
                return NotFound();
            }

            var currentSubscription = user.Subscriptions
                .OrderByDescending(x => x.StartedAt)
                .FirstOrDefault();

            // charger les features du plan actif
            var features = currentSubscription?.ApplicationPlan?.FeatureValues
                .ToDictionary(
                    fv => fv.ApplicationFeature.Key,
                    fv => fv.Value)
                ?? new Dictionary<string, string>();

            var status = new ApplicationUserStatusDto
            {
                ApplicationId = user.ApplicationId,
                ApplicationUserId = user.Id,
                SubscriptionId = currentSubscription?.Id ?? Guid.Empty,
                ApplicationPlanId = currentSubscription?.ApplicationPlanId ?? Guid.Empty,
                IsActive = user.IsActive,
                Plan = currentSubscription?.ApplicationPlan?.Name,
                SubscriptionStatus = currentSubscription?.Status.ToString(),
                TrialEndAt = currentSubscription?.TrialEndAt,
                CurrentPeriodEnd = currentSubscription?.CurrentPeriodEnd,
                Features = features 
            };

            return Ok(status);
        }
    }
}

