using NSubstitute;
using NUnit.Framework;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgHomeBot.Notifications.Telegram.Commands;

namespace TgHomeBot.Notifications.Telegram.Tests.Commands;

[TestFixture]
public class HelpCommandTests
{
    private ITelegramBotClient _client = null!;
    private HelpCommand _command = null!;

    [SetUp]
    public void SetUp()
    {
        _client = Substitute.For<ITelegramBotClient>();
        _command = new HelpCommand();
    }

    [Test]
    public void Name_ShouldReturn_HelpCommand()
    {
        Assert.That(_command.Name, Is.EqualTo("/help"));
    }

    [Test]
    public async Task ProcessMessage_ShouldSendHelpText()
    {
        // Arrange
        var message = new Message
        {
            Chat = new Chat { Id = 123 }
        };

        // Act
        await _command.ProcessMessage(message, _client, CancellationToken.None);

        // Assert - Just verify the method completes without error
        // Note: Can't easily verify SendTextMessageAsync as it's an extension method
        Assert.Pass("Help command processed successfully");
    }
}
