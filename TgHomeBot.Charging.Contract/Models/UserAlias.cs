namespace TgHomeBot.Charging.Contract.Models;

/// <summary>
/// User alias configuration for charging sessions
/// </summary>
public class UserAlias
{
    /// <summary>
    /// The user ID from Easee
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Human-readable alias for the user
    /// </summary>
    public string? Alias { get; set; }

    /// <summary>
    /// List of alphanumeric token IDs associated with this user
    /// </summary>
    public List<string> TokenIds { get; set; } = [];
}
