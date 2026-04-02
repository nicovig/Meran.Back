    namespace Meran.Back.DTO
    {
        public class SubscriptionDto
        {
            public Guid Id { get; set; }
            public Guid ApplicationUserId { get; set; }
            public Guid ApplicationPlanId { get; set; }
            public string Status { get; set; } = null!;
            public DateTime StartedAt { get; set; }
            public DateTime? EndedAt { get; set; }
            public DateTime CurrentPeriodEnd { get; set; }
            public DateTime? TrialEndAt { get; set; }
        }

        public class CreateSubscriptionRequestDto
        {
            public Guid ApplicationPlanId { get; set; }
            public string Status { get; set; } = null!;
            public DateTime StartedAt { get; set; }
            public DateTime? EndedAt { get; set; }
            public DateTime CurrentPeriodEnd { get; set; }
            public DateTime? TrialEndAt { get; set; }
        }
    }
