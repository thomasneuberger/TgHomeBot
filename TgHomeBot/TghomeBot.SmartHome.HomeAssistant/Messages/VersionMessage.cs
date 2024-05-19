using System.Text.Json.Serialization;

namespace TgHomeBot.SmartHome.HomeAssistant.Messages;

public class VersionMessage : IMessage
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("ha_version")]
    public required string HomeAssistantVersion { get; init; }
}