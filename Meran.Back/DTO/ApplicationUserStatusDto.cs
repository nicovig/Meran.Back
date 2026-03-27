namespace Meran.Back.DTO
{
    public class ApplicationUserStatusDto
    {
        public Guid ApplicationId { get; set; }
        public Guid ApplicationUserId { get; set; }
        public Guid ApplicationPlanId { get; set; }
        public bool IsActive { get; set; }
        public string? Plan { get; set; }
        public DateTime? LastPaymentAt { get; set; }
        public DateTime? NextPaymentDueAt { get; set; }
        public bool HasPaymentIssue { get; set; }
    }
}

