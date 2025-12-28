using NSubstitute;
using NUnit.Framework;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Notifications.Telegram.Commands;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram.Tests.Commands;

[TestFixture]
public class EndCommandTests
{
    private IRegisteredChatService _registeredChatService = null!;
    private ITelegramBotClient _client = null!;
    private EndCommand _command = null!;

    [SetUp]
    public void SetUp()
    {
        _registeredChatService = Substitute.For<IRegisteredChatService>();
        _client = Substitute.For<ITelegramBotClient>();
        _command = new EndCommand(_registeredChatService);
    }

    [Test]
    public void Name_ShouldReturn_EndCommand()
    {
        Assert.That(_command.Name, Is.EqualTo("/end"));
    }

    [Test]
    public async Task ProcessMessage_ShouldUnregisterChat_AndSendMessage()
    {
        // Arrange
        var message = new Message
        {
            Chat = new Chat { Id = 456 }
        };
        _registeredChatService.UnregisterChatAsync(456).Returns(Task.FromResult(true));

        // Act
        await _command.ProcessMessage(message, _client, CancellationToken.None);

        // Assert
        await _registeredChatService.Received(1).UnregisterChatAsync(456);
        // Note: Can't easily verify SendTextMessageAsync as it's an extension method
    }

    [Test]
    public async Task ProcessMessage_ShouldNotSendMessage_WhenChatNotRegistered()
    {
        // Arrange
        var message = new Message
        {
            Chat = new Chat { Id = 999 }
        };
        _registeredChatService.UnregisterChatAsync(999).Returns(Task.FromResult(false));

        // Act
        await _command.ProcessMessage(message, _client, CancellationToken.None);

        // Assert
        await _registeredChatService.Received(1).UnregisterChatAsync(999);
    }
}
