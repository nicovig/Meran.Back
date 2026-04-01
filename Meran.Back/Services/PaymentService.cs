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
                .Include(x => x.Subscriptions)
                .SingleOrDefaultAsync(x => x.ApplicationId == applicationId && x.Id == applicationUserId, cancellationToken);

            if (user == null)
            {
                return null;
            }

            var subscription = user.Subscriptions
                .OrderByDescending(x => x.StartedAt)
                .FirstOrDefault();

            if (subscription == null)
            {
                return null;
            }

            var eventType = ParsePaymentEventType(request.Type);
            var status = request.Status ?? "processed";

            var evt = new PaymentEvent
            {
                Id = Guid.NewGuid(),
                ApplicationId = applicationId,
                SubscriptionId = subscription.Id,
                EventType = eventType,
                Status = status,
                Amount = request.Amount,
                Currency = request.Currency,
                OccurredAt = request.OccurredAt,
                Provider = request.Provider,
                ProviderReference = request.ProviderReference,
                RawPayload = request.RawPayload
            };

            _dbContext.PaymentEvents.Add(evt);

            if (eventType == PaymentEventType.PaymentSucceeded || eventType == PaymentEventType.InvoiceCreated)
            {
                user.IsActive = true;
                subscription.Status = SubscriptionStatus.Active;

                if (request.NextPaymentDueAt.HasValue)
                {
                    subscription.CurrentPeriodEnd = request.NextPaymentDueAt.Value;
                }
            }

            if (eventType == PaymentEventType.PaymentFailed)
            {
                subscription.Status = SubscriptionStatus.PastDue;
            }

            if (eventType == PaymentEventType.Refund)
            {
                subscription.Status = SubscriptionStatus.Canceled;
                subscription.EndedAt = request.OccurredAt;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return ToDto(evt);
        }

        public async Task<PaymentsOverviewDto> GetOverviewAsync(Guid? applicationId, CancellationToken cancellationToken)
        {
            IQueryable<PaymentEvent> paymentsQuery = _dbContext.PaymentEvents
                .AsNoTracking()
                .Include(x => x.Subscription);
            var subscriptionsQuery = _dbContext.Subscriptions
                .AsNoTracking()
                .Include(x => x.ApplicationPlan)
                .Include(x => x.ApplicationUser)
                .ThenInclude(x => x.Application)
                .AsQueryable();

            if (applicationId.HasValue)
            {
                paymentsQuery = paymentsQuery.Where(x => x.ApplicationId == applicationId.Value);
                subscriptionsQuery = subscriptionsQuery.Where(x => x.ApplicationUser.ApplicationId == applicationId.Value);
            }

            var payments = await paymentsQuery
                .OrderByDescending(x => x.OccurredAt)
                .ToListAsync(cancellationToken);

            var subscriptions = await subscriptionsQuery.ToListAsync(cancellationToken);

            var overview = new PaymentsOverviewDto
            {
                PastPayments = payments.Select(ToDto).ToList(),
                UpcomingPayments = subscriptions
                    .Where(x => x.Status == SubscriptionStatus.Active && x.CurrentPeriodEnd > DateTime.UtcNow)
                    .Select(x => new ScheduledPaymentDto
                    {
                        ApplicationId = x.ApplicationUser.ApplicationId,
                        ApplicationUserId = x.ApplicationUserId,
                        SubscriptionId = x.Id,
                        NextPaymentDueAt = x.CurrentPeriodEnd,
                        Plan = x.ApplicationPlan.Name,
                        ExpectedAmount = x.ApplicationPlan.Price,
                        Currency = "EUR"
                    })
                    .ToList()
            };

            return overview;
        }

        private static PaymentEventType ParsePaymentEventType(string value)
        {
            return value switch
            {
                "initial" => PaymentEventType.PaymentSucceeded,
                "recurring" => PaymentEventType.PaymentSucceeded,
                "failed" => PaymentEventType.PaymentFailed,
                "canceled" => PaymentEventType.Refund,
                "refunded" => PaymentEventType.Refund,
                "paymentSucceeded" => PaymentEventType.PaymentSucceeded,
                "paymentFailed" => PaymentEventType.PaymentFailed,
                "refund" => PaymentEventType.Refund,
                "invoiceCreated" => PaymentEventType.InvoiceCreated,
                _ => throw new ArgumentOutOfRangeException(nameof(value), "Unsupported payment event type.")
            };
        }

        private static string PaymentEventTypeToString(PaymentEventType type)
        {
            return type switch
            {
                PaymentEventType.PaymentSucceeded => "paymentSucceeded",
                PaymentEventType.PaymentFailed => "paymentFailed",
                PaymentEventType.Refund => "refund",
                PaymentEventType.InvoiceCreated => "invoiceCreated",
                _ => "paymentSucceeded"
            };
        }

        private static PaymentEventDto ToDto(PaymentEvent evt)
        {
            return new PaymentEventDto
            {
                Id = evt.Id,
                ApplicationId = evt.ApplicationId,
                ApplicationUserId = evt.Subscription.ApplicationUserId,
                SubscriptionId = evt.SubscriptionId,
                EventType = PaymentEventTypeToString(evt.EventType),
                Status = evt.Status,
                Amount = evt.Amount,
                Currency = evt.Currency,
                OccurredAt = evt.OccurredAt,
                Provider = evt.Provider,
                ProviderReference = evt.ProviderReference
            };
        }
    }
}

