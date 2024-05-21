using System.Text.Json.Serialization;

namespace TgHomeBot.SmartHome.HomeAssistant.Messages;
public class Event<TData>
{
	[JsonPropertyName("event_type")]
	public required string EventType { get; init; }

	[JsonPropertyName("time_fired")]
	public required DateTimeOffset TimeFired { get; init; }

	[JsonPropertyName("origin")]
	public required string Origin { get; init; }

	[JsonPropertyName("data")]
	public required TData Data { get; init; }

}
