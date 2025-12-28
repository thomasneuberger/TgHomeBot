using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Charging.Contract.Requests;
using TgHomeBot.Charging.Contract.Services;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class MonthlyReportCommand(IServiceProvider serviceProvider) : ICommand
{
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
            await client.SendTextMessageAsync(new ChatId(message.Chat.Id),
                $"❌ Fehler beim Abrufen der Ladevorgänge:\n{result.ErrorMessage}",
                cancellationToken: cancellationToken);
            return;
        }

        var sessions = result.Data!;
        var report = formatter.FormatMonthlyReport(sessions);

        await client.SendTextMessageAsync(new ChatId(message.Chat.Id), report, cancellationToken: cancellationToken);
    }
}
