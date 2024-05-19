using System.Text.Json.Serialization;

namespace TgHomeBot.SmartHome.HomeAssistant.Models;

public class HomeAssistantDeviceAttributes
{
    [JsonPropertyName("state_class")]
    public string? StateClass { get; init; }

    [JsonPropertyName("model_type")]
    public string? ModelType { get; init; }

    [JsonPropertyName("connection_type")]
    public string? ConnectionType { get; init; }

    [JsonPropertyName("rssi_device")]
    public int? RssiDevice { get; init; }

    [JsonPropertyName("rssi_peer")]
    public int? RssiPeer { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("is_group")]
    public bool? IsGroup { get; init; }

    [JsonPropertyName("unit_of_measurement")]
    public string? UnitOfMeasurement { get; init; }

    [JsonPropertyName("device_class")]
    public string? DeviceClass { get; init; }

    [JsonPropertyName("friendly_name")]
    public required string FriendlyName { get; init; }
}