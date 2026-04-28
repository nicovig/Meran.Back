using Meran.Back.Models;

namespace Meran.Back.DTO
{
    public class ApplicationPlanDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string BillingPeriod { get; set; } = null!;
        public decimal Price { get; set; }
    }

    public class ApplicationUserDto
    {
        public Guid Id { get; set; }
        public Guid ApplicationId { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Origin { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string? Plan { get; set; }
    }
        

    public class ApplicationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Format { get; set; } = null!;
        public decimal? OneShotPrice { get; set; }
        public List<ApplicationPlanDto> Plans { get; set; } = new();
        public List<ApplicationUserDto> Users { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

    public class CreateApplicationRequestDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Format { get; set; } = null!;
        public decimal? OneShotPrice { get; set; }
        public List<ApplicationPlanDto>? Plans { get; set; }
    }

    public class UpdateApplicationRequestDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Format { get; set; } = null!;
        public decimal? OneShotPrice { get; set; }
        public List<ApplicationPlanDto>? Plans { get; set; }
    }

    public class AddApplicationUserRequestDto
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Plan { get; set; }
    }

    public class ApplicationLoginRequestDto
    {
        public Guid ApplicationId { get; set; }
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class ApplicationAuthResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }
        public ApplicationUserDto User { get; set; } = null!;
        public string Plan { get; set; } = null!;
        public Dictionary<string, string> Features { get; set; } = new();
    }
}

