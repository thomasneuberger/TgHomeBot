using System.Text.Json.Serialization;

namespace TgHomeBot.Charging.Easee.Models;

/// <summary>
/// Easee charging session information
/// </summary>
internal class EaseeChargingSession
{
    [JsonPropertyName("id")]
    public required int Id { get; set; }

    [JsonPropertyName("carConnected")]
    public required DateTime CarConnected { get; set; }

    [JsonPropertyName("carDisconnected")]
    public DateTime? CarDisconnected { get; set; }

    [JsonPropertyName("kiloWattHours")]
    public required double KiloWattHours { get; set; }

    [JsonPropertyName("authUser")]
    public int UserId { get; set; } = 0;

    [JsonPropertyName("sessionEnergyDetails")]
    public EaseeSessionEnergyDetails? SessionEnergyDetails { get; set; }
}

/// <summary>
/// Energy details for a charging session
/// </summary>
internal class EaseeSessionEnergyDetails
{
    [JsonPropertyName("actualDuration")]
    public int? ActualDuration { get; set; }
}
