namespace TgHomeBot.Charging.Contract.Models;

/// <summary>
/// Request model for authentication
/// </summary>
public class AuthenticationRequest
{
    public required string UserName { get; set; }
    public required string Password { get; set; }
}
