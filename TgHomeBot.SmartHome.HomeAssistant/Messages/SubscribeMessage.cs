using System.Text.Json.Serialization;

namespace TgHomeBot.SmartHome.HomeAssistant.Messages;

public class SubscribeMessage : IMessage
{
    internal const int StateChangedId = 1;
    internal const string StateChangedEventType = "state_changed";

    [JsonPropertyName("type")]
    public string Type => "subscribe_events";

    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("event_type")]
    public required string EventType { get; init; }
}