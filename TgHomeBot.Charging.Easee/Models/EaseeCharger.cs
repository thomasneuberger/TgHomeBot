using System.Text.Json.Serialization;

namespace TgHomeBot.Charging.Easee.Models;

/// <summary>
/// Easee charger information
/// </summary>
internal class EaseeCharger
{
    [JsonPropertyName("id")]
    public required int Id { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }
}
