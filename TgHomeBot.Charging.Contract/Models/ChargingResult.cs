namespace TgHomeBot.Charging.Contract.Models;

/// <summary>
/// Result wrapper for API operations that can fail
/// </summary>
public class ChargingResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorMessage { get; set; }

    public static ChargingResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static ChargingResult<T> Error(string message) => new() { Success = false, ErrorMessage = message };
}
