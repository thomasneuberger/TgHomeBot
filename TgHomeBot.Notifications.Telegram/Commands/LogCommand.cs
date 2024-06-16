using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Common.Contract;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class LogCommand(IRegisteredChatService registeredChatService, ILogFileProvider logFileProvider, ILogger<LogCommand> logger) : ICommand
{
    public string Name => "/logs";

    public string Description => "Die Logdateien auflisten";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        var isRegistered = registeredChatService.RegisteredChats.Any(c => c.ChatId == message.Chat.Id);

        if (isRegistered)
        {
            var logFiles = logFileProvider.GetLogFileList()
                .OrderDescending()
                .Select(f => $"/logfile_{GetFileCommandName(f)}")
                .ToList();
            var response = $"""
                           Those log files are available:
                           {string.Join('\n', logFiles)}
                           """;
            await client.SendTextMessageAsync(new ChatId(message.Chat.Id), response, cancellationToken: cancellationToken);
        }
        else
        {
            await client.SendTextMessageAsync(new ChatId(message.Chat.Id), "Es besteht keine Verbindung zum TgHomeBot", cancellationToken: cancellationToken);
        }
    }

    private static string GetFileCommandName(string filename)
    {
        return Path.GetFileNameWithoutExtension(filename).Replace("-", "");
    }
}
