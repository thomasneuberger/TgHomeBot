using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Scheduling.Contract;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class ScheduledTasksCommand(IServiceProvider serviceProvider) : ICommand
{
    public string Name => "/tasks";
    public string Description => "Geplante Aufgaben auflisten";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var schedulerService = scope.ServiceProvider.GetRequiredService<ISchedulerService>();

        var tasks = schedulerService.GetScheduledTasks().ToList();

        if (tasks.Count == 0)
        {
            await client.SendMessage(
                new ChatId(message.Chat.Id),
                "Keine geplanten Aufgaben gefunden.",
                cancellationToken: cancellationToken);
            return;
        }

        var taskMessages = tasks.Select(t =>
        {
            var status = t.Enabled ? "✅ Aktiviert" : "❌ Deaktiviert";
            var nextRun = t.NextExecutionTime.HasValue
                ? t.NextExecutionTime.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm:ss")
                : "Nicht geplant";

            return $"<b>{t.TaskName}</b>\n" +
                   $"Typ: <code>{t.TaskType}</code>\n" +
                   $"Status: {status}\n" +
                   $"Zeitplan: <code>{t.CronExpression}</code>\n" +
                   $"Nächste Ausführung: {nextRun}";
        });

        var responseMessage = "<b>📋 Geplante Aufgaben:</b>\n\n" + string.Join("\n\n", taskMessages);

        await client.SendMessage(
            new ChatId(message.Chat.Id),
            responseMessage,
            parseMode: global::Telegram.Bot.Types.Enums.ParseMode.Html,
            cancellationToken: cancellationToken);
    }
}
