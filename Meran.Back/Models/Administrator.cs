namespace Meran.Back.Models
{
    public class Administrator
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Salt { get; set; } = null!;
        public string? DisplayName { get; set; }
        public string Role { get; set; } = "Admin";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

