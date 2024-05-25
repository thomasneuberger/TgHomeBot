using System.Text.Json.Serialization;

namespace TgHomeBot.SmartHome.HomeAssistant.Messages;

public class EventMessage<TData> : IMessage
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("id")]
    public required int Id { get; init; }

    [JsonPropertyName("event")]
    public required Event<TData> Event { get; set; }
}