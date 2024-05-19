using System.Text.Json.Serialization;

namespace TgHomeBot.SmartHome.HomeAssistant.Messages;

public class ResultMessage : IMessage
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("success")]
    public required bool Success { get; set; }

    [JsonIgnore]
    public string? CompleteMessage { get; set; }
}