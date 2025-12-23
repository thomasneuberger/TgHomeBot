namespace TgHomeBot.Charging.Contract;

/// <summary>
/// Interface for charging station connectors
/// </summary>
public interface IChargingConnector
{
    /// <summary>
    /// Authenticates with the charging provider API
    /// </summary>
    /// <param name="username">Username for authentication</param>
    /// <param name="password">Password for authentication</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if authentication was successful, false otherwise</returns>
    Task<bool> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the authentication token
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if refresh was successful, false otherwise</returns>
    Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the connector is currently authenticated
    /// </summary>
    /// <returns>True if authenticated, false otherwise</returns>
    bool IsAuthenticated { get; }
}
