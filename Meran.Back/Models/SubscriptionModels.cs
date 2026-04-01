namespace Meran.Back.Models
{
    public enum SubscriptionStatus
    {
        Active,
        Canceled,
        Trialing,
        PastDue
    }

    public class Subscription
    {
        public Guid Id { get; set; }
        public Guid ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; } = null!;

        public Guid ApplicationPlanId { get; set; }
        public ApplicationPlan ApplicationPlan { get; set; } = null!;

        public SubscriptionStatus Status { get; set; }

        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public DateTime CurrentPeriodEnd { get; set; }
        public DateTime? TrialEndAt { get; set; }

        public ICollection<PaymentEvent> PaymentEvents { get; set; } = new List<PaymentEvent>();
    }
}

