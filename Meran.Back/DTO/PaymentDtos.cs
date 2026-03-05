namespace Meran.Back.DTO
{
    public class PaymentEventDto
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid ApplicationUserId { get; set; }
        public string Type { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public DateTime OccurredAt { get; set; }
        public string? Provider { get; set; }
        public string? ProviderReference { get; set; }
    }

    public class CreatePaymentEventRequestDto
    {
        public string Type { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public DateTime OccurredAt { get; set; }
        public string? Provider { get; set; }
        public string? ProviderReference { get; set; }
        public string? RawPayload { get; set; }
        public DateTime? NextPaymentDueAt { get; set; }
    }

    public class ScheduledPaymentDto
    {
        public Guid ApplicationId { get; set; }
        public Guid ApplicationUserId { get; set; }
        public DateTime NextPaymentDueAt { get; set; }
        public string? Plan { get; set; }
        public decimal? ExpectedAmount { get; set; }
        public string? Currency { get; set; }
    }

    public class PaymentsOverviewDto
    {
        public List<PaymentEventDto> PastPayments { get; set; } = new();
        public List<ScheduledPaymentDto> UpcomingPayments { get; set; } = new();
    }

    public class ApplicationUserPaymentIssueDto
    {
        public Guid ApplicationId { get; set; }
        public Guid ApplicationUserId { get; set; }
        public string ApplicationName { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string UserEmail { get; set; } = null!;
        public string? Plan { get; set; }
        public DateTime? LastPaymentAt { get; set; }
        public DateTime? NextPaymentDueAt { get; set; }
    }
}

