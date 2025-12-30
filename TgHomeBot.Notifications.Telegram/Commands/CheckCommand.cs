using MediatR;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Notifications.Telegram.Services;
using TgHomeBot.SmartHome.Contract.Requests;

namespace TgHomeBot.Notifications.Telegram.Commands;

internal class CheckCommand(IRegisteredChatService registeredChatService, IMediator mediator) : ICommand
{
    public bool AllowUnregistered => true;

    public string Name => "/check";

    public string Description => "Prüfen ob eie Verbindung mit dem Bot besteht.";

    public async Task ProcessMessage(Message message, ITelegramBotClient client, CancellationToken cancellationToken)
    {
        var isRegistered = registeredChatService.RegisteredChats.Any(c => c.ChatId == message.Chat.Id);

        if (isRegistered)
        {
            var state = await mediator.Send(new GetMonitorStateRequest(), cancellationToken);
            var replyMessage = $"""
                          Es besteht eine Verbindung zum TgHomeBot.
                          Der Status des Smart Home Monitorings ist {state}.
                          """;
            await client.SendMessage(new ChatId(message.Chat.Id), replyMessage, cancellationToken: cancellationToken);
        }
        else
        {
            await client.SendMessage(new ChatId(message.Chat.Id), "Es besteht keine Verbindung zum TgHomeBot", cancellationToken: cancellationToken);
        }
    }
}
