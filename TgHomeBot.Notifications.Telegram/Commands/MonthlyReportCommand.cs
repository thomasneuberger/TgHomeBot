using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Charging.Contract.Requests;
using TgHomeBot.Charging.Contract.Services;
using TgHomeBot.Common.Contract;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class MonthlyReportCommand(IServiceProvider serviceProvider, IOptions<ApplicationOptions> applicationOptions) : ICommand
{
    private readonly ApplicationOptions _applicationOptions = applicationOptions.Value;

    public string Name => "/monthlyreport";

    public string Description => "Monatliche Zusammenfassung des geladenen Stroms";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var formatter = scope.ServiceProvider.GetRequiredService<IMonthlyReportFormatter>();

        // Get sessions for the last two months
        var to = DateTime.UtcNow.Date;
        var from = new DateTime(to.Year, to.Month, 1).AddMonths(-2); // First day of 2 months ago

        var result = await mediator.Send(new GetChargingSessionsRequest(from, to), cancellationToken);

        if (!result.Success)
        {
            await client.SendMessage(new ChatId(message.Chat.Id),
                $"❌ Fehler beim Abrufen der Ladevorgänge:\n{result.ErrorMessage}",
                cancellationToken: cancellationToken);
            
            // If it's an authentication error, send the login URL as a separate message
            if (result.ErrorMessage?.Contains("Nicht mit Easee API authentifiziert") == true)
            {
                var loginUrl = $"{_applicationOptions.BaseUrl.TrimEnd('/')}/Easee/Login";
                await client.SendMessage(new ChatId(message.Chat.Id),
                    loginUrl,
                    cancellationToken: cancellationToken);
            }
            return;
        }

        var sessions = result.Data!;
        var report = formatter.FormatMonthlyReport(sessions);

        await client.SendMessage(new ChatId(message.Chat.Id), report, cancellationToken: cancellationToken);
    }
}
