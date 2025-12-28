using NSubstitute;
using NUnit.Framework;
using TgHomeBot.Notifications.Contract;
using TgHomeBot.Notifications.Contract.Requests;
using TgHomeBot.Notifications.Telegram.RequestHandlers;

namespace TgHomeBot.Notifications.Telegram.Tests.RequestHandlers;

[TestFixture]
public class NotifyRequestHandlerTests
{
    private INotificationConnector _connector = null!;
    private NotifyRequestHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _connector = Substitute.For<INotificationConnector>();
        _handler = new NotifyRequestHandler(_connector);
    }

    [Test]
    public async Task Handle_ShouldCallSendAsync_WithMessageAndGeneralType()
    {
        // Arrange
        var request = new NotifyRequest("Test message");
        _connector.SendAsync(Arg.Any<string>(), Arg.Any<NotificationType>()).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        await _connector.Received(1).SendAsync("Test message", NotificationType.General);
    }

    [Test]
    public async Task Handle_ShouldCallSendAsync_WithSpecificNotificationType()
    {
        // Arrange
        var request = new NotifyRequest("Device notification", NotificationType.DeviceNotification);
        _connector.SendAsync(Arg.Any<string>(), Arg.Any<NotificationType>()).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(request, CancellationToken.None);

        // Assert
        await _connector.Received(1).SendAsync("Device notification", NotificationType.DeviceNotification);
    }

    [Test]
    public async Task Handle_ShouldCompleteSuccessfully_WhenConnectorSucceeds()
    {
        // Arrange
        var request = new NotifyRequest("Success message");
        _connector.SendAsync(Arg.Any<string>(), Arg.Any<NotificationType>()).Returns(Task.CompletedTask);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await _handler.Handle(request, CancellationToken.None));
    }
}
