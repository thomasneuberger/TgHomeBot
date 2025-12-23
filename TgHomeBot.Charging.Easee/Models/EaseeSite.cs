using System.Text.Json.Serialization;

namespace TgHomeBot.Charging.Easee.Models;

/// <summary>
/// Easee site information containing circuits
/// </summary>
internal class EaseeSite
{
    [JsonPropertyName("circuits")]
    public required List<EaseeCircuit> Circuits { get; set; }
}

/// <summary>
/// Easee circuit information containing chargers
/// </summary>
internal class EaseeCircuit
{
    [JsonPropertyName("chargers")]
    public required List<EaseeCharger> Chargers { get; set; }
}
