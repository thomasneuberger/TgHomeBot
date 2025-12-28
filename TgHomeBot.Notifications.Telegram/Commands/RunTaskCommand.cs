using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Scheduling.Contract;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class RunTaskCommand(IServiceProvider serviceProvider) : ICommand
{
    public string Name => "/runtask";
    public string Description => "Eine geplante Aufgabe sofort ausführen";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        // Extract task type from message
        var parts = message.Text?.Split('_', StringSplitOptions.RemoveEmptyEntries);
        
        if (parts == null || parts.Length < 2)
        {
            using var scope = serviceProvider.CreateScope();
            var schedulerService = scope.ServiceProvider.GetRequiredService<ISchedulerService>();
            var tasks = schedulerService.GetScheduledTasks().ToList();

            if (tasks.Count == 0)
            {
                await client.SendTextMessageAsync(
                    new ChatId(message.Chat.Id),
                    "Keine geplanten Aufgaben verfügbar.",
                    cancellationToken: cancellationToken);
                return;
            }

            var taskList = string.Join("\n", tasks.Select(t => 
                $"• {Name}_{t.TaskType} - {t.TaskName}"));
            var helpMessage = "<b>Verwendung:</b>\n" +
                            $"<code>{Name}_TaskType</code>\n\n" +
                            "<b>Verfügbare Aufgaben:</b>\n" +
                            $"Klicken Sie auf einen Befehl um ihn auszuführen:\n{taskList}";

            await client.SendTextMessageAsync(
                new ChatId(message.Chat.Id),
                helpMessage,
                parseMode: global::Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }

        var taskType = StripBotName(parts[1]);

        using var executionScope = serviceProvider.CreateScope();
        var scheduler = executionScope.ServiceProvider.GetRequiredService<ISchedulerService>();

        // Execute the task
        var success = await scheduler.RunTaskNowAsync(taskType);

        if (success)
        {
            await client.SendTextMessageAsync(
                new ChatId(message.Chat.Id),
                $"✅ Aufgabe <code>{taskType}</code> wurde erfolgreich ausgeführt.",
                parseMode: global::Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken: cancellationToken);
        }
        else
        {
            await client.SendTextMessageAsync(
                new ChatId(message.Chat.Id),
                $"❌ Fehler beim Ausführen der Aufgabe <code>{taskType}</code>. Überprüfen Sie die Logs für Details.",
                parseMode: global::Telegram.Bot.Types.Enums.ParseMode.Html,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Removes the bot name suffix from a parameter (e.g., "TaskName@botname" -> "TaskName")
    /// This is necessary because Telegram appends @botname to commands in group chats
    /// </summary>
    private static string StripBotName(string parameter)
    {
        if (string.IsNullOrEmpty(parameter))
        {
            return parameter;
        }

        var atIndex = parameter.IndexOf('@');
        return atIndex > 0 ? parameter[..atIndex] : parameter;
    }
}
