using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using TgHomeBot.Notifications.Contract;

namespace TgHomeBot.Scheduling.Tasks;

/// <summary>
/// Scheduled task to report the current Eurojackpot lottery jackpot to Telegram
/// Runs every Tuesday and Friday at 10pm (22:00)
/// </summary>
public class JackpotReportTask : IScheduledTask
{
    private readonly ILogger<JackpotReportTask> _logger;
    private readonly INotificationConnector _notificationConnector;
    private readonly IHttpClientFactory _httpClientFactory;

    public string TaskName => "JackpotReportTask";

    public JackpotReportTask(
        ILogger<JackpotReportTask> logger,
        INotificationConnector notificationConnector,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationConnector = notificationConnector ?? throw new ArgumentNullException(nameof(notificationConnector));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Eurojackpot jackpot report task");

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync("https://lottoapi.herokuapp.com/eurojackpot-results/1", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch Eurojackpot data. Status code: {StatusCode}", response.StatusCode);
                return;
            }

            var results = await response.Content.ReadFromJsonAsync<List<EurojackpotResult>>(cancellationToken);

            if (results == null || results.Count == 0)
            {
                _logger.LogWarning("No Eurojackpot results found");
                return;
            }

            var latestResult = results[0];
            var message = FormatJackpotMessage(latestResult);

            await _notificationConnector.SendAsync(message);
            _logger.LogInformation("Successfully sent Eurojackpot jackpot report");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing Eurojackpot jackpot report task");
        }
    }

    private static string FormatJackpotMessage(EurojackpotResult result)
    {
        var message = $"🎰 <b>Eurojackpot Ziehung</b>\n\n";
        message += $"📅 Datum: {result.Date}\n";
        message += $"💰 Jackpot: {FormatJackpotAmount(result.Jackpot)}\n\n";
        message += $"🔢 Gewinnzahlen: {string.Join(", ", result.Numbers)}\n";
        message += $"⭐ Eurozahlen: {string.Join(", ", result.EuroNumbers)}\n\n";
        message += "Viel Glück! 🍀";
        
        return message;
    }

    private static string FormatJackpotAmount(string jackpot)
    {
        // The API returns jackpot as a string, format it nicely
        if (string.IsNullOrEmpty(jackpot))
        {
            return "Nicht verfügbar";
        }

        // Try to parse and format as currency
        if (decimal.TryParse(jackpot.Replace("€", "").Replace(",", "").Trim(), out var amount))
        {
            return $"{amount:N0} €";
        }

        return jackpot;
    }

    private class EurojackpotResult
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("numbers")]
        public List<int> Numbers { get; set; } = new();

        [JsonPropertyName("euroNumbers")]
        public List<int> EuroNumbers { get; set; } = new();

        [JsonPropertyName("jackpot")]
        public string Jackpot { get; set; } = string.Empty;
    }
}
