using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Common.Contract;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class LogFileCommand(IRegisteredChatService registeredChatService, ILogFileProvider logFileProvider) : ICommand
{
    public string Name => "/logfile";
    public string Description => "Eine bestimmte Logdatei herunterladen";
    public bool HideFromMenu => true;
    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        var isRegistered = registeredChatService.RegisteredChats.Any(c => c.ChatId == message.Chat.Id);

        if (isRegistered)
        {
            var command = CommandHelper.StripBotName(message.Text?.Split('_').LastOrDefault() ?? string.Empty);
            var filename = logFileProvider.GetLogFileList()
                .FirstOrDefault(f => command == GetFileCommandName(f));
            if (string.IsNullOrWhiteSpace(filename))
            {
                await client.SendMessage(new ChatId(message.Chat.Id), "Datei nicht gefunden.", cancellationToken: cancellationToken);
                return;
            }

            var contentStream = logFileProvider.GetLogFileContent(filename, cancellationToken);

            if (contentStream is null)
            {
                await client.SendMessage(new ChatId(message.Chat.Id), "Datei konnte nicht gelesen werden.", cancellationToken: cancellationToken);
                return;
            }

            var file = new InputFileStream(contentStream, filename);
            await client.SendDocument(new ChatId(message.Chat.Id), file, cancellationToken: cancellationToken);
        }
        else
        {
            await client.SendMessage(new ChatId(message.Chat.Id), "Es besteht keine Verbindung zum TgHomeBot", cancellationToken: cancellationToken);
        }
    }

    private static string GetFileCommandName(string filename)
    {
        return Path.GetFileNameWithoutExtension(filename).Replace("-", "");
    }
}