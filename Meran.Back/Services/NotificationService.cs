using Microsoft.EntityFrameworkCore;
using Meran.Back.Data;
using Meran.Back.DTO;
using Meran.Back.Models;

namespace Meran.Back.Services
{
    public interface INotificationService
    {
        Task<List<ApplicationUserPaymentIssueDto>> CheckPaymentIssuesAsync(CancellationToken cancellationToken);
        Task<List<ApplicationUserPaymentIssueDto>> GetCurrentPaymentIssuesAsync(CancellationToken cancellationToken);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _dbContext;

        public NotificationService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<ApplicationUserPaymentIssueDto>> CheckPaymentIssuesAsync(CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var subscriptions = await _dbContext.Subscriptions
                .Include(x => x.ApplicationPlan)
                .Include(x => x.ApplicationUser)
                .ThenInclude(x => x.Application)
                .Where(x => x.Status == SubscriptionStatus.Active && x.CurrentPeriodEnd < now)
                .ToListAsync(cancellationToken);

            foreach (var subscription in subscriptions)
            {
                subscription.Status = SubscriptionStatus.PastDue;
                subscription.ApplicationUser.IsActive = false;
            }

            if (subscriptions.Count > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return subscriptions.Select(x => new ApplicationUserPaymentIssueDto
            {
                ApplicationId = x.ApplicationUser.ApplicationId,
                ApplicationUserId = x.ApplicationUserId,
                SubscriptionId = x.Id,
                ApplicationName = x.ApplicationUser.Application.Name,
                UserName = x.ApplicationUser.Name,
                UserEmail = x.ApplicationUser.Email,
                Plan = x.ApplicationPlan.Name,
                TrialEndAt = x.TrialEndAt,
                CurrentPeriodEnd = x.CurrentPeriodEnd
            }).ToList();
        }

        public async Task<List<ApplicationUserPaymentIssueDto>> GetCurrentPaymentIssuesAsync(CancellationToken cancellationToken)
        {
            var subscriptions = await _dbContext.Subscriptions
                .AsNoTracking()
                .Include(x => x.ApplicationPlan)
                .Include(x => x.ApplicationUser)
                .ThenInclude(x => x.Application)
                .Where(x => x.Status == SubscriptionStatus.PastDue)
                .ToListAsync(cancellationToken);

            return subscriptions.Select(x => new ApplicationUserPaymentIssueDto
            {
                ApplicationId = x.ApplicationUser.ApplicationId,
                ApplicationUserId = x.ApplicationUserId,
                SubscriptionId = x.Id,
                ApplicationName = x.ApplicationUser.Application.Name,
                UserName = x.ApplicationUser.Name,
                UserEmail = x.ApplicationUser.Email,
                Plan = x.ApplicationPlan.Name,
                TrialEndAt = x.TrialEndAt,
                CurrentPeriodEnd = x.CurrentPeriodEnd
            }).ToList();
        }
    }
}

