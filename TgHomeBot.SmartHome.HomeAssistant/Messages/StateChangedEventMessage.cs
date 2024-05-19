using System.Text.Json.Serialization;
using TgHomeBot.SmartHome.HomeAssistant.Models;

namespace TgHomeBot.SmartHome.HomeAssistant.Messages;

public class StateChangedEventMessage : EventMessage
{
    [JsonPropertyName("data")]
    public required StateChangedData Data { get; init; }

    public class StateChangedData
    {
        [JsonPropertyName("entity_id")]
        public required string EntityId { get; set; }

        [JsonPropertyName("old_state")]
        public required HomeAssistantDevice OldState { get; set; }

        [JsonPropertyName("new_state")]
        public required HomeAssistantDevice NewState { get; set; }
    }
}