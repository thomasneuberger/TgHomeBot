using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TgHomeBot.Charging.Contract;
using TgHomeBot.Charging.Contract.Models;
using TgHomeBot.Charging.Easee.Models;
using TgHomeBot.Common.Contract;

namespace TgHomeBot.Charging.Easee;

/// <summary>
/// Connector for Easee API
/// </summary>
internal class EaseeConnector : IChargingConnector
{
    private readonly ILogger<EaseeConnector> _logger;
    private readonly HttpClient _httpClient;
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly string _tokenFilePath;
    private EaseeTokenData? _tokenData;
    private readonly object _lock = new();

    public EaseeConnector(
        ILogger<EaseeConnector> logger,
        IHttpClientFactory httpClientFactory,
        IOptions<EaseeOptions> easeeOptions,
        IOptions<FileStorageOptions> fileStorageOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClientFactory?.CreateClient() ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _fileStorageOptions = fileStorageOptions?.Value ?? throw new ArgumentNullException(nameof(fileStorageOptions));
        
        var options = easeeOptions?.Value ?? throw new ArgumentNullException(nameof(easeeOptions));
        _httpClient.BaseAddress = new Uri(options.BaseUrl);
        
        _tokenFilePath = Path.Combine(_fileStorageOptions.Path, "easee-token.json");
        LoadTokenFromFile();
    }

    public bool IsAuthenticated
    {
        get
        {
            lock (_lock)
            {
                return _tokenData != null && _tokenData.ExpiresAt > DateTime.UtcNow;
            }
        }
    }

    public async Task<bool> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Authenticating with Easee API for user {Username}", username);

            var request = new
            {
                userName = username,
                password = password
            };

            var response = await _httpClient.PostAsJsonAsync("/api/accounts/login", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Authentication failed with status {StatusCode}: {ErrorContent}", 
                    response.StatusCode, errorContent);
                return false;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<EaseeAuthResponse>(cancellationToken);
            
            if (authResponse == null)
            {
                _logger.LogError("Failed to deserialize authentication response");
                return false;
            }

            lock (_lock)
            {
                _tokenData = new EaseeTokenData
                {
                    AccessToken = authResponse.AccessToken,
                    RefreshToken = authResponse.RefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn)
                };
            }

            SaveTokenToFile();
            _logger.LogInformation("Successfully authenticated with Easee API");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Easee authentication");
            return false;
        }
    }

    public async Task<bool> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        string? refreshToken;
        
        lock (_lock)
        {
            if (_tokenData == null)
            {
                _logger.LogWarning("No token data available for refresh");
                return false;
            }
            refreshToken = _tokenData.RefreshToken;
        }

        try
        {
            _logger.LogInformation("Refreshing Easee access token");

            // According to Easee API documentation, the accessToken field should be empty string
            // when refreshing tokens. Only the refreshToken is needed.
            var request = new
            {
                accessToken = string.Empty,
                refreshToken = refreshToken
            };

            var response = await _httpClient.PostAsJsonAsync("/api/accounts/refresh_token", request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Token refresh failed with status {StatusCode}: {ErrorContent}", 
                    response.StatusCode, errorContent);
                return false;
            }

            var authResponse = await response.Content.ReadFromJsonAsync<EaseeAuthResponse>(cancellationToken);
            
            if (authResponse == null)
            {
                _logger.LogError("Failed to deserialize token refresh response");
                return false;
            }

            lock (_lock)
            {
                _tokenData = new EaseeTokenData
                {
                    AccessToken = authResponse.AccessToken,
                    RefreshToken = authResponse.RefreshToken,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(authResponse.ExpiresIn)
                };
            }

            SaveTokenToFile();
            _logger.LogInformation("Successfully refreshed Easee access token");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Easee token refresh");
            return false;
        }
    }

    private void LoadTokenFromFile()
    {
        try
        {
            if (!File.Exists(_tokenFilePath))
            {
                _logger.LogInformation("No existing token file found at {Path}", _tokenFilePath);
                return;
            }

            var json = File.ReadAllText(_tokenFilePath);
            var tokenData = JsonSerializer.Deserialize<EaseeTokenData>(json);
            
            if (tokenData != null)
            {
                lock (_lock)
                {
                    _tokenData = tokenData;
                }
                _logger.LogInformation("Loaded token from file, expires at {ExpiresAt}", tokenData.ExpiresAt);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading token from file");
        }
    }

    private void SaveTokenToFile()
    {
        try
        {
            EaseeTokenData? tokenData;
            lock (_lock)
            {
                tokenData = _tokenData;
            }

            if (tokenData == null)
            {
                return;
            }

            var directory = Path.GetDirectoryName(_tokenFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(tokenData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            // Write to temporary file first, then move atomically
            var tempFilePath = $"{_tokenFilePath}.tmp";
            File.WriteAllText(tempFilePath, json);
            File.Move(tempFilePath, _tokenFilePath, overwrite: true);
            
            _logger.LogInformation("Saved token to file {Path}", _tokenFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving token to file");
        }
    }

    public async Task<ChargingResult<IReadOnlyList<ChargerInfo>>> GetChargersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            _logger.LogInformation("Fetching chargers from Easee API");

            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/accounts/chargers");
            AddAuthorizationHeader(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to get chargers with status {StatusCode}: {ErrorContent}",
                    response.StatusCode, errorContent);
                return ChargingResult<IReadOnlyList<ChargerInfo>>.Error($"Fehler beim Abrufen der Ladestationen (HTTP {response.StatusCode})");
            }

            var sites = await response.Content.ReadFromJsonAsync<List<EaseeSite>>(cancellationToken);

            if (sites == null)
            {
                _logger.LogError("Failed to deserialize chargers response");
                return ChargingResult<IReadOnlyList<ChargerInfo>>.Error("Fehler beim Verarbeiten der Ladestationen-Daten");
            }

            var chargers = sites
                .SelectMany(s => s.Circuits)
                .SelectMany(c => c.Chargers)
                .Select(ch => new ChargerInfo
                {
                    Id = ch.Id,
                    Name = ch.Name
                })
                .ToList();

            _logger.LogInformation("Successfully fetched {Count} chargers", chargers.Count);
            return ChargingResult<IReadOnlyList<ChargerInfo>>.Ok(chargers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching chargers");
            return ChargingResult<IReadOnlyList<ChargerInfo>>.Error($"Fehler beim Abrufen der Ladestationen: {ex.Message}");
        }
    }

    public async Task<ChargingResult<IReadOnlyList<string>>> GetChargerIdsAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetChargersAsync(cancellationToken);
        if (!result.Success)
        {
            return ChargingResult<IReadOnlyList<string>>.Error(result.ErrorMessage!);
        }

        var ids = result.Data!.Select(c => c.Id).ToList();
        return ChargingResult<IReadOnlyList<string>>.Ok(ids);
    }

    public async Task<ChargingResult<IReadOnlyList<ChargingSession>>> GetChargingSessionsAsync(string chargerId, string chargerName, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            _logger.LogInformation("Fetching charging sessions for charger {ChargerName} ({ChargerId}) from {From} to {To}",
                chargerName, chargerId, from, to);

            var fromStr = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var toStr = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var url = $"/api/sessions/charger/{chargerId}/sessions/{fromStr}/{toStr}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddAuthorizationHeader(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to get charging sessions for {ChargerName} with status {StatusCode}: {ErrorContent}",
                    chargerName, response.StatusCode, errorContent);
                return ChargingResult<IReadOnlyList<ChargingSession>>.Error($"Fehler beim Abrufen der Ladevorgänge für {chargerName} (HTTP {response.StatusCode})");
            }

            var sessions = await response.Content.ReadFromJsonAsync<List<EaseeChargingSession>>(cancellationToken);

            if (sessions == null)
            {
                _logger.LogError("Failed to deserialize charging sessions response for {ChargerName}", chargerName);
                return ChargingResult<IReadOnlyList<ChargingSession>>.Error($"Fehler beim Verarbeiten der Ladevorgänge-Daten für {chargerName}");
            }

            _logger.LogInformation("Successfully fetched {Count} charging sessions for charger {ChargerName}",
                sessions.Count, chargerName);

            var chargingSessions = sessions.Select(s => new ChargingSession
            {
                UserId = s.UserId.ToString(),
                CarConnected = s.CarConnected,
                CarDisconnected = s.CarDisconnected,
                KiloWattHours = s.KiloWattHours,
                ActualDurationSeconds = s.SessionEnergyDetails?.ActualDuration
            }).ToList();

            return ChargingResult<IReadOnlyList<ChargingSession>>.Ok(chargingSessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching charging sessions for charger {ChargerName}", chargerName);
            return ChargingResult<IReadOnlyList<ChargingSession>>.Error($"Fehler beim Abrufen der Ladevorgänge für {chargerName}: {ex.Message}");
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (!IsAuthenticated)
        {
            var refreshed = await RefreshTokenAsync(cancellationToken);
            if (!refreshed)
            {
                throw new InvalidOperationException("Not authenticated with Easee API. Please authenticate first.");
            }
        }
    }

    private void AddAuthorizationHeader(HttpRequestMessage request)
    {
        string? accessToken;
        lock (_lock)
        {
            accessToken = _tokenData?.AccessToken;
        }

        if (accessToken != null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}
