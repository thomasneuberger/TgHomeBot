using System.Text.Json.Serialization;

namespace TgHomeBot.SmartHome.HomeAssistant.Messages;

public class GenericMessage : IMessage
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }
}