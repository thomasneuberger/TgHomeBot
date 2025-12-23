namespace TgHomeBot.Charging.Easee.Models;

/// <summary>
/// Token data for persistent storage
/// </summary>
internal class EaseeTokenData
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public required DateTime ExpiresAt { get; set; }
}
