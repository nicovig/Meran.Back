using Microsoft.EntityFrameworkCore;
using Meran.Back.Data;
using Meran.Back.DTO;

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

            var users = await _dbContext.ApplicationUsers
                .Include(u => u.Application)
                .Where(u => u.IsActive && u.NextPaymentDueAt.HasValue && u.NextPaymentDueAt < now)
                .ToListAsync(cancellationToken);

            foreach (var user in users)
            {
                user.IsActive = false;
                user.HasPaymentIssue = true;
            }

            if (users.Count > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return users.Select(u => new ApplicationUserPaymentIssueDto
            {
                ApplicationId = u.ApplicationId,
                ApplicationUserId = u.Id,
                ApplicationName = u.Application.Name,
                UserName = u.Name,
                UserEmail = u.Email,
                Plan = u.Plan,
                LastPaymentAt = u.LastPaymentAt,
                NextPaymentDueAt = u.NextPaymentDueAt
            }).ToList();
        }

        public async Task<List<ApplicationUserPaymentIssueDto>> GetCurrentPaymentIssuesAsync(CancellationToken cancellationToken)
        {
            var users = await _dbContext.ApplicationUsers
                .AsNoTracking()
                .Include(u => u.Application)
                .Where(u => u.HasPaymentIssue)
                .ToListAsync(cancellationToken);

            return users.Select(u => new ApplicationUserPaymentIssueDto
            {
                ApplicationId = u.ApplicationId,
                ApplicationUserId = u.Id,
                ApplicationName = u.Application.Name,
                UserName = u.Name,
                UserEmail = u.Email,
                Plan = u.Plan,
                LastPaymentAt = u.LastPaymentAt,
                NextPaymentDueAt = u.NextPaymentDueAt
            }).ToList();
        }
    }
}

