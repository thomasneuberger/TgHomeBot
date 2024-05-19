using System.Text.Json.Serialization;

namespace TghomeBot.SmartHome.HomeAssistant.Models;

public class HomeAssistantDeviceAttributes
{
    [JsonPropertyName("state_class")]
    public string? StateClass { get; set; }

    [JsonPropertyName("model_type")]
    public string? ModelType { get; set; }

    [JsonPropertyName("connection_type")]
    public string? ConnectionType { get; set; }

    [JsonPropertyName("rssi_device")]
    public int? RssiDevice { get; set; }

    [JsonPropertyName("rssi_peer")]
    public int? RssiPeer { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("is_group")]
    public bool? IsGroup { get; set; }

    [JsonPropertyName("unit_of_measurement")]
    public string? UnitOfMeasurement { get; set; }

    [JsonPropertyName("device_class")]
    public string? DeviceClass { get; set; }

    [JsonPropertyName("friendly_name")]
    public required string FriendlyName { get; set; }
}