namespace Meran.Back.Models
{
    public enum ApplicationFormat
    {
        Free,
        OneShot,
        Subscription
    }

    public enum BillingPeriod
    {
        Monthly,
        Quarterly,
        Semiannual,
        Annual,
        Biennial
    }

    public enum UserOrigin
    {
        Admin,
        Self
    }

    public enum ApplicationFeatureType
    {
        Integer,
        Boolean,
        String
    }

    public enum ApplicationMembershipRole
    {
        Admin,
        Member,
        Viewer
    }

    public class Application
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public ApplicationFormat Format { get; set; }
        public decimal? OneShotPrice { get; set; }
        public DateTime CreatedAt { get; set; }

        public ICollection<ApplicationPlan> Plans { get; set; } = new List<ApplicationPlan>();
        public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public ICollection<ApplicationFeature> Features { get; set; } = new List<ApplicationFeature>();
    }

    public class ApplicationPlan
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public BillingPeriod BillingPeriod { get; set; }
        public decimal Price { get; set; }

        public Application Application { get; set; } = null!;
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public ICollection<ApplicationPlanFeatureValue> FeatureValues { get; set; } = new List<ApplicationPlanFeatureValue>();
    }

    public class ApplicationUser
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public UserOrigin Origin { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Plan { get; set; }
        public DateTime? LastPaymentAt { get; set; }
        public DateTime? NextPaymentDueAt { get; set; }
        public string? PaymentProvider { get; set; }
        public string? PaymentReference { get; set; }
        public decimal? LastPaymentAmount { get; set; }
        public string? LastPaymentCurrency { get; set; }
        public bool HasPaymentIssue { get; set; }

        public Guid ApplicationId { get; set; }
        public Application Application { get; set; } = null!;
        public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
        public ICollection<ApplicationUserRole> Roles { get; set; } = new List<ApplicationUserRole>();
    }

    public class ApplicationFeature
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public string Key { get; set; } = null!;
        public ApplicationFeatureType Type { get; set; }

        public Application Application { get; set; } = null!;
        public ICollection<ApplicationPlanFeatureValue> PlanValues { get; set; } = new List<ApplicationPlanFeatureValue>();
    }

    public class ApplicationPlanFeatureValue
    {
        public Guid ApplicationPlanId { get; set; }
        public Guid ApplicationFeatureId { get; set; }
        public string Value { get; set; } = null!;

        public ApplicationPlan ApplicationPlan { get; set; } = null!;
        public ApplicationFeature ApplicationFeature { get; set; } = null!;
    }

    public class ApplicationUserRole
    {
        public Guid ApplicationUserId { get; set; }
        public ApplicationMembershipRole Role { get; set; }

        public ApplicationUser ApplicationUser { get; set; } = null!;
    }
}

