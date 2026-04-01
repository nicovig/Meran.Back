namespace Meran.Back.Models
{
    public enum PaymentEventType
    {
        PaymentSucceeded,
        PaymentFailed,
        Refund,
        InvoiceCreated,
        Initial = PaymentSucceeded,
        Recurring = PaymentSucceeded,
        Failed = PaymentFailed,
        Canceled = Refund,
        Refunded = Refund
    }

    public class PaymentEvent
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid ApplicationUserId { get; set; }
        public Guid SubscriptionId { get; set; }
        public PaymentEventType EventType { get; set; }
        public PaymentEventType Type
        {
            get => EventType;
            set => EventType = value;
        }
        public string Status { get; set; } = null!;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public DateTime OccurredAt { get; set; }
        public string? Provider { get; set; }
        public string? ProviderReference { get; set; }
        public string? RawPayload { get; set; }

        public Application Application { get; set; } = null!;
        public Subscription Subscription { get; set; } = null!;
    }
}

