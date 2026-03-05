using Microsoft.EntityFrameworkCore;
using Meran.Back.Data;
using Meran.Back.DTO;
using Meran.Back.Models;

namespace Meran.Back.Services
{
    public interface IPaymentService
    {
        Task<PaymentEventDto?> AddPaymentAsync(Guid applicationId, Guid applicationUserId, CreatePaymentEventRequestDto request, CancellationToken cancellationToken);
        Task<PaymentsOverviewDto> GetOverviewAsync(Guid? applicationId, CancellationToken cancellationToken);
    }

    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _dbContext;

        public PaymentService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<PaymentEventDto?> AddPaymentAsync(Guid applicationId, Guid applicationUserId, CreatePaymentEventRequestDto request, CancellationToken cancellationToken)
        {
            var user = await _dbContext.ApplicationUsers
                .SingleOrDefaultAsync(x => x.ApplicationId == applicationId && x.Id == applicationUserId, cancellationToken);

            if (user == null)
            {
                return null;
            }

            var type = ParsePaymentEventType(request.Type);

            var evt = new PaymentEvent
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                ApplicationUserId = applicationUserId,
                Type = type,
                Amount = request.Amount,
                Currency = request.Currency,
                OccurredAt = request.OccurredAt,
                Provider = request.Provider,
                ProviderReference = request.ProviderReference,
                RawPayload = request.RawPayload
            };

            _dbContext.PaymentEvents.Add(evt);

            if (type == PaymentEventType.Initial || type == PaymentEventType.Recurring)
            {
                user.IsActive = true;
                user.LastPaymentAt = request.OccurredAt;
                user.LastPaymentAmount = request.Amount;
                user.LastPaymentCurrency = request.Currency;
                user.PaymentProvider = request.Provider;
                user.PaymentReference = request.ProviderReference;
                user.HasPaymentIssue = false;

                if (request.NextPaymentDueAt.HasValue)
                {
                    user.NextPaymentDueAt = request.NextPaymentDueAt;
                }
            }

            if (type == PaymentEventType.Canceled || type == PaymentEventType.Failed)
            {
                user.HasPaymentIssue = true;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ToDto(evt);
        }

        public async Task<PaymentsOverviewDto> GetOverviewAsync(Guid? applicationId, CancellationToken cancellationToken)
        {
            var paymentsQuery = _dbContext.PaymentEvents.AsNoTracking();
            var usersQuery = _dbContext.ApplicationUsers.AsNoTracking().Include(u => u.Application).AsQueryable();

            if (applicationId.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(x => x.ApplicationId == applicationId.Value);
                usersQuery = usersQuery.Where(x => x.ApplicationId == applicationId.Value);
            }

            var payments = await paymentsQuery
                .OrderByDescending(x => x.OccurredAt)
                .ToListAsync(cancellationToken);

            var users = await usersQuery.ToListAsync(cancellationToken);

            var overview = new PaymentsOverviewDto
            {
                PastPayments = payments.Select(ToDto).ToList(),
                UpcomingPayments = users
                    .Where(u => u.IsActive && u.NextPaymentDueAt.HasValue)
                    .Select(u => new ScheduledPaymentDto
                    {
                        ApplicationId = u.ApplicationId,
                        ApplicationUserId = u.Id,
                        NextPaymentDueAt = u.NextPaymentDueAt!.Value,
                        Plan = u.Plan,
                        ExpectedAmount = u.LastPaymentAmount,
                        Currency = u.LastPaymentCurrency
                    })
                    .ToList()
            };

            return overview;
        }

        private static PaymentEventType ParsePaymentEventType(string value)
        {
            return value switch
            {
                "initial" => PaymentEventType.Initial,
                "recurring" => PaymentEventType.Recurring,
                "failed" => PaymentEventType.Failed,
                "canceled" => PaymentEventType.Canceled,
                "refunded" => PaymentEventType.Refunded,
                _ => throw new ArgumentOutOfRangeException(nameof(value), "Unsupported payment event type.")
            };
        }

        private static string PaymentEventTypeToString(PaymentEventType type)
        {
            return type switch
            {
                PaymentEventType.Initial => "initial",
                PaymentEventType.Recurring => "recurring",
                PaymentEventType.Failed => "failed",
                PaymentEventType.Canceled => "canceled",
                PaymentEventType.Refunded => "refunded",
                _ => "initial"
            };
        }

        private static PaymentEventDto ToDto(PaymentEvent evt)
        {
            return new PaymentEventDto
            {
                Id = evt.Id,
                ApplicationId = evt.ApplicationId,
                ApplicationUserId = evt.ApplicationUserId,
                Type = PaymentEventTypeToString(evt.Type),
                Amount = evt.Amount,
                Currency = evt.Currency,
                OccurredAt = evt.OccurredAt,
                Provider = evt.Provider,
                ProviderReference = evt.ProviderReference
            };
        }
    }
}

