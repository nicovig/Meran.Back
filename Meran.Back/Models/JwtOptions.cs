namespace Meran.Back.Models
{
    public class JwtOptions
    {
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string SigningKey { get; set; } = null!;
        public int AccessTokenExpiresMinutes { get; set; } = 120;
    }
}

