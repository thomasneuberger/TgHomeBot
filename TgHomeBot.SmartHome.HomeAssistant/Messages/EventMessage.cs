using System.Text.Json.Serialization;

namespace TgHomeBot.SmartHome.HomeAssistant.Messages;

public class EventMessage : IMessage
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("event_type")]
    public required string EventType { get; init; }

    [JsonPropertyName("time_fired")]
    public required DateTimeOffset TimeFired { get; init; }

    [JsonPropertyName("origin")]
    public required string Origin { get; init; }
}