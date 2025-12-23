namespace TgHomeBot.Charging.Contract.Models;

/// <summary>
/// Charging session information
/// </summary>
public class ChargingSession
{
    public required string UserId { get; set; }
    public required DateTime CarConnected { get; set; }
    public DateTime? CarDisconnected { get; set; }
    public required double KiloWattHours { get; set; }
    public int? ActualDurationSeconds { get; set; }
}
