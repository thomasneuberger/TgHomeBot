using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgHomeBot.Charging.Contract;
using TgHomeBot.Charging.Contract.Models;
using TgHomeBot.Common.Contract;

namespace TgHomeBot.Charging.Easee;

/// <summary>
/// Service for managing user aliases for Easee charging sessions
/// </summary>
internal class UserAliasService : IUserAliasService
{
    private readonly ILogger<UserAliasService> _logger;
    private readonly string _userIdsFilePath;
    private readonly string _aliasesFilePath;
    private readonly object _lock = new();
    private HashSet<string> _trackedUserIds;
    private List<UserAlias> _aliases;

    public UserAliasService(
        ILogger<UserAliasService> logger,
        IOptions<FileStorageOptions> fileStorageOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var options = fileStorageOptions?.Value ?? throw new ArgumentNullException(nameof(fileStorageOptions));

        _userIdsFilePath = Path.Combine(options.Path, "easee-users.json");
        _aliasesFilePath = Path.Combine(options.Path, "easee-user-aliases.json");

        _trackedUserIds = [];
        _aliases = [];

        LoadTrackedUserIds();
        LoadAliases();
    }

    public IReadOnlyList<UserAlias> GetAllAliases()
    {
        lock (_lock)
        {
            return _aliases.ToList();
        }
    }

    public UserAlias? GetAliasByUserId(string userId)
    {
        lock (_lock)
        {
            return _aliases.FirstOrDefault(a => a.UserId == userId);
        }
    }

    public void SaveAlias(UserAlias userAlias)
    {
        if (userAlias == null)
        {
            throw new ArgumentNullException(nameof(userAlias));
        }

        lock (_lock)
        {
            var existing = _aliases.FirstOrDefault(a => a.UserId == userAlias.UserId);
            if (existing != null)
            {
                _aliases.Remove(existing);
            }

            _aliases.Add(userAlias);
            SaveAliasesToFile();
        }
    }

    public void DeleteAlias(string userId)
    {
        lock (_lock)
        {
            var existing = _aliases.FirstOrDefault(a => a.UserId == userId);
            if (existing != null)
            {
                _aliases.Remove(existing);
                SaveAliasesToFile();
            }
        }
    }

    public string ResolveUserName(string userId, string? authToken = null)
    {
        lock (_lock)
        {
            // First, try to find a direct match by user ID
            var alias = _aliases.FirstOrDefault(a => a.UserId == userId);
            if (alias?.Alias != null)
            {
                return alias.Alias;
            }

            // If authToken is provided, try to find a match by token ID
            if (!string.IsNullOrEmpty(authToken))
            {
                alias = _aliases.FirstOrDefault(a => a.TokenIds.Contains(authToken));
                if (alias?.Alias != null)
                {
                    return alias.Alias;
                }
            }

            // Fall back to user ID
            return userId;
        }
    }

    public void TrackUserId(string userId)
    {
        lock (_lock)
        {
            if (_trackedUserIds.Add(userId))
            {
                SaveTrackedUserIdsToFile();
            }
        }
    }

    public IReadOnlyList<string> GetTrackedUserIds()
    {
        lock (_lock)
        {
            return _trackedUserIds.OrderBy(id => id).ToList();
        }
    }

    private void LoadTrackedUserIds()
    {
        try
        {
            if (!File.Exists(_userIdsFilePath))
            {
                _logger.LogInformation("No existing user IDs file found at {Path}", _userIdsFilePath);
                return;
            }

            var json = File.ReadAllText(_userIdsFilePath);
            var userIds = JsonSerializer.Deserialize<List<string>>(json);

            if (userIds != null)
            {
                _trackedUserIds = new HashSet<string>(userIds);
                _logger.LogInformation("Loaded {Count} tracked user IDs from file", _trackedUserIds.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tracked user IDs from file");
        }
    }

    private void SaveTrackedUserIdsToFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(_userIdsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var userIdsList = _trackedUserIds.OrderBy(id => id).ToList();
            var json = JsonSerializer.Serialize(userIdsList, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var tempFilePath = $"{_userIdsFilePath}.tmp";
            File.WriteAllText(tempFilePath, json);
            File.Move(tempFilePath, _userIdsFilePath, overwrite: true);

            _logger.LogInformation("Saved {Count} tracked user IDs to file {Path}", userIdsList.Count, _userIdsFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving tracked user IDs to file");
        }
    }

    private void LoadAliases()
    {
        try
        {
            if (!File.Exists(_aliasesFilePath))
            {
                _logger.LogInformation("No existing aliases file found at {Path}", _aliasesFilePath);
                return;
            }

            var json = File.ReadAllText(_aliasesFilePath);
            var aliases = JsonSerializer.Deserialize<List<UserAlias>>(json);

            if (aliases != null)
            {
                _aliases = aliases;
                _logger.LogInformation("Loaded {Count} user aliases from file", _aliases.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user aliases from file");
        }
    }

    private void SaveAliasesToFile()
    {
        try
        {
            var directory = Path.GetDirectoryName(_aliasesFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_aliases, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var tempFilePath = $"{_aliasesFilePath}.tmp";
            File.WriteAllText(tempFilePath, json);
            File.Move(tempFilePath, _aliasesFilePath, overwrite: true);

            _logger.LogInformation("Saved {Count} user aliases to file {Path}", _aliases.Count, _aliasesFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user aliases to file");
        }
    }
}
