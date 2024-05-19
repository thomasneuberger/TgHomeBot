using System.Text.Json.Serialization;

namespace TgHomeBot.SmartHome.HomeAssistant.Messages;

public class AuthInvalidMessage : IMessage
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; set; }
}