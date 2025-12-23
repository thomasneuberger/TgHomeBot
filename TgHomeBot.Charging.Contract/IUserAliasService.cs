using TgHomeBot.Charging.Contract.Models;

namespace TgHomeBot.Charging.Contract;

/// <summary>
/// Service for managing user aliases for charging sessions
/// </summary>
public interface IUserAliasService
{
    /// <summary>
    /// Get all user aliases
    /// </summary>
    IReadOnlyList<UserAlias> GetAllAliases();

    /// <summary>
    /// Get a specific user alias by user ID
    /// </summary>
    UserAlias? GetAliasByUserId(string userId);

    /// <summary>
    /// Save or update a user alias
    /// </summary>
    void SaveAlias(UserAlias userAlias);

    /// <summary>
    /// Delete a user alias
    /// </summary>
    void DeleteAlias(string userId);

    /// <summary>
    /// Resolve user ID to display name
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="authToken">Optional auth token for token-based matching</param>
    /// <returns>Alias if found, otherwise the user ID</returns>
    string ResolveUserName(string userId, string? authToken = null);

    /// <summary>
    /// Track a user ID from a charging session
    /// </summary>
    void TrackUserId(string userId);

    /// <summary>
    /// Get all tracked user IDs
    /// </summary>
    IReadOnlyList<string> GetTrackedUserIds();
}
