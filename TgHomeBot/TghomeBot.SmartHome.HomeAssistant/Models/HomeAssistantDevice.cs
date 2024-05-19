using System.Text.Json.Serialization;

namespace TghomeBot.SmartHome.HomeAssistant.Models;

public class HomeAssistantDevice
{
    [JsonPropertyName("entity_id")]
    public required string EntityId { get; set; }

    [JsonPropertyName("state")]
    public required string State { get; set; }

    [JsonPropertyName("attributes")]
    public required HomeAssistantDeviceAttributes Attributes { get; set; }
}