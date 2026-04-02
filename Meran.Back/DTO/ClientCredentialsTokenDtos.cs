namespace Meran.Back.DTO
{
    public class ClientCredentialsTokenRequestDto
    {
        public string grant_type { get; set; } = null!;
        public string client_id { get; set; } = null!;
        public string client_secret { get; set; } = null!;
    }

    public class ClientCredentialsTokenResponseDto
    {
        public string access_token { get; set; } = null!;
        public string token_type { get; set; } = "Bearer";
        public int expires_in { get; set; }
    }
}
