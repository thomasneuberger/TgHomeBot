using TgHomeBot.Charging.Contract.Models;

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

    /// <summary>
    /// Gets all charger IDs available to the authenticated user
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of charger IDs</returns>
    Task<IReadOnlyList<string>> GetChargerIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets charging sessions for a specific charger within a date range
    /// </summary>
    /// <param name="chargerId">ID of the charger</param>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of charging sessions</returns>
    Task<IReadOnlyList<ChargingSession>> GetChargingSessionsAsync(string chargerId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
