using System.Text.Json.Serialization;

namespace TgHomeBot.Charging.Easee.Models;

/// <summary>
/// Response from Easee authentication endpoint
/// </summary>
internal class EaseeAuthResponse
{
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; set; }

    [JsonPropertyName("expiresIn")]
    public required int ExpiresIn { get; set; }

    [JsonPropertyName("tokenType")]
    public required string TokenType { get; set; }

    [JsonPropertyName("refreshToken")]
    public required string RefreshToken { get; set; }
}
