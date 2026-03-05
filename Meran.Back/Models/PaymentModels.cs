namespace Meran.Back.Models
{
    public enum PaymentEventType
    {
        Initial,
        Recurring,
        Failed,
        Canceled,
        Refunded
    }

    public class PaymentEvent
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid ApplicationUserId { get; set; }
        public PaymentEventType Type { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public DateTime OccurredAt { get; set; }
        public string? Provider { get; set; }
        public string? ProviderReference { get; set; }
        public string? RawPayload { get; set; }

        public Application Application { get; set; } = null!;
        public ApplicationUser ApplicationUser { get; set; } = null!;
    }
}

