namespace Meran.Back.Models
{
    public class MachineClientOptions
    {
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string Role { get; set; } = "ApiClient";
        public int AccessTokenExpiresMinutes { get; set; } = 60;
    }
}
