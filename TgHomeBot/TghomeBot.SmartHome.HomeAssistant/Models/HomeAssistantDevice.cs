using System.Text.Json.Serialization;

namespace TgHomeBot.SmartHome.HomeAssistant.Models;

public class HomeAssistantDevice
{
    [JsonPropertyName("entity_id")]
    public required string EntityId { get; init; }

    [JsonPropertyName("state")]
    public required string State { get; init; }

    [JsonPropertyName("attributes")]
    public required HomeAssistantDeviceAttributes Attributes { get; init; }
}