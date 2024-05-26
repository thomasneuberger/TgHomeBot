using Telegram.Bot;
using Telegram.Bot.Types;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal interface ICommand
{
    bool AllowUnregistered => false;
    string Name { get; }
    string Description { get; }
    Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken);
}
