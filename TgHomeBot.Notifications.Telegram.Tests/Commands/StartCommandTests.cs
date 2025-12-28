using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Notifications.Telegram.Commands;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram.Tests.Commands;

[TestFixture]
public class StartCommandTests
{
    private IOptions<TelegramOptions> _options = null!;
    private IRegisteredChatService _registeredChatService = null!;
    private ITelegramBotClient _client = null!;
    private StartCommand _command = null!;

    [SetUp]
    public void SetUp()
    {
        var telegramOptions = new TelegramOptions
        {
            Token = "test-token",
            AllowedUserNames = new[] { "alloweduser" }
        };
        _options = Options.Create(telegramOptions);
        _registeredChatService = Substitute.For<IRegisteredChatService>();
        _client = Substitute.For<ITelegramBotClient>();
        _command = new StartCommand(_options, _registeredChatService);
    }

    [Test]
    public void Name_ShouldReturn_StartCommand()
    {
        Assert.That(_command.Name, Is.EqualTo("/start"));
    }

    [Test]
    public void AllowUnregistered_ShouldReturnTrue()
    {
        Assert.That(_command.AllowUnregistered, Is.True);
    }

    [Test]
    public async Task ProcessMessage_ShouldRegisterChat_WhenUserIsAllowed()
    {
        // Arrange
        var message = new Message
        {
            From = new User { Id = 123, Username = "alloweduser" },
            Chat = new Chat { Id = 456, Title = "Test Chat" }
        };
        _registeredChatService.RegisterChat(123, "alloweduser", 456, "Test Chat").Returns(Task.FromResult(true));

        // Act
        await _command.ProcessMessage(message, _client, CancellationToken.None);

        // Assert
        await _registeredChatService.Received(1).RegisterChat(123, "alloweduser", 456, "Test Chat");
        // Note: We can't easily verify TelegramBotClient.SendTextMessageAsync due to it being an extension method
    }

    [Test]
    public async Task ProcessMessage_ShouldNotRegisterChat_WhenUserIsNotAllowed()
    {
        // Arrange
        var message = new Message
        {
            From = new User { Id = 123, Username = "notallowed" },
            Chat = new Chat { Id = 456 }
        };

        // Act
        await _command.ProcessMessage(message, _client, CancellationToken.None);

        // Assert
        await _registeredChatService.DidNotReceive().RegisterChat(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string?>());
    }

    [Test]
    public async Task ProcessMessage_ShouldNotRegisterChat_WhenUsernameIsNull()
    {
        // Arrange
        var message = new Message
        {
            From = new User { Id = 123, Username = null },
            Chat = new Chat { Id = 456 }
        };

        // Act
        await _command.ProcessMessage(message, _client, CancellationToken.None);

        // Assert
        await _registeredChatService.DidNotReceive().RegisterChat(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string?>());
    }

    [Test]
    public async Task ProcessMessage_ShouldNotSendMessage_WhenAlreadyRegistered()
    {
        // Arrange
        var message = new Message
        {
            From = new User { Id = 123, Username = "alloweduser" },
            Chat = new Chat { Id = 456, Title = "Test Chat" }
        };
        _registeredChatService.RegisterChat(123, "alloweduser", 456, "Test Chat").Returns(Task.FromResult(false));

        // Act
        await _command.ProcessMessage(message, _client, CancellationToken.None);

        // Assert
        await _registeredChatService.Received(1).RegisterChat(123, "alloweduser", 456, "Test Chat");
        // Note: Can't verify SendTextMessageAsync easily as it's an extension method
    }
}
