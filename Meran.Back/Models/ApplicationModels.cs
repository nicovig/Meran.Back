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
    }

    public class ApplicationUser
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public UserOrigin Origin { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Plan { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastPaymentAt { get; set; }
        public DateTime? NextPaymentDueAt { get; set; }
        public string? PaymentProvider { get; set; }
        public string? PaymentReference { get; set; }
        public decimal? LastPaymentAmount { get; set; }
        public string? LastPaymentCurrency { get; set; }
        public bool HasPaymentIssue { get; set; }

        public Application Application { get; set; } = null!;
    }
}

