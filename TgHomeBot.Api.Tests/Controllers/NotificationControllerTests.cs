using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NUnit.Framework;
using TgHomeBot.Api.Controllers;
using TgHomeBot.Notifications.Contract.Requests;
using TgHomeBot.Notifications.Telegram.Models;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Api.Tests.Controllers;

[TestFixture]
public class NotificationControllerTests
{
    private IMediator _mediator = null!;
    private IRegisteredChatService _registeredChatService = null!;
    private NotificationController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _mediator = Substitute.For<IMediator>();
        _registeredChatService = Substitute.For<IRegisteredChatService>();
        _controller = new NotificationController(_mediator, _registeredChatService);
    }

    [Test]
    public async Task SendMessage_ShouldSendNotifyRequest_AndReturnOk()
    {
        // Arrange
        const string message = "Test notification";
        _mediator.Send(Arg.Any<NotifyRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(Unit.Value));

        // Act
        var result = await _controller.SendMessage(message);

        // Assert
        Assert.That(result, Is.InstanceOf<OkResult>());
        await _mediator.Received(1).Send(Arg.Is<NotifyRequest>(r => r.Message == message), Arg.Any<CancellationToken>());
    }

    [Test]
    public void GetRegisteredChats_ShouldReturnAllChats()
    {
        // Arrange
        var chats = new List<RegisteredChat>
        {
            new() { Id = 1, Username = "user1", ChatId = 100, ChatName = "Chat 1" },
            new() { Id = 2, Username = "user2", ChatId = 200, ChatName = "Chat 2" }
        }.AsReadOnly();
        _registeredChatService.RegisteredChats.Returns(chats);

        // Act
        var result = _controller.GetRegisteredChats();

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var returnedChats = okResult!.Value as IEnumerable<object>;
        Assert.That(returnedChats, Is.Not.Null);
        Assert.That(returnedChats!.Count(), Is.EqualTo(2));
    }

    [Test]
    public void GetChatFlags_ShouldReturnFlags_WhenChatExists()
    {
        // Arrange
        const long chatId = 100;
        var chat = new RegisteredChat
        {
            Id = 1,
            Username = "user1",
            ChatId = chatId,
            ChatName = "Chat 1",
            EurojackpotEnabled = true,
            MonthlyChargingReportEnabled = false,
            DeviceNotificationsEnabled = true
        };
        _registeredChatService.GetRegisteredChat(chatId).Returns(chat);

        // Act
        var result = _controller.GetChatFlags(chatId);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public void GetChatFlags_ShouldReturnNotFound_WhenChatDoesNotExist()
    {
        // Arrange
        const long chatId = 999;
        _registeredChatService.GetRegisteredChat(chatId).Returns((RegisteredChat?)null);

        // Act
        var result = _controller.GetChatFlags(chatId);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task ToggleEurojackpot_ShouldToggleFlag_AndReturnSuccess()
    {
        // Arrange
        const long chatId = 100;
        _registeredChatService.ToggleEurojackpotAsync(chatId).Returns(Task.FromResult(true));
        var chat = new RegisteredChat
        {
            Id = 1,
            Username = "user1",
            ChatId = chatId,
            EurojackpotEnabled = false
        };
        _registeredChatService.GetRegisteredChat(chatId).Returns(chat);

        // Act
        var result = await _controller.ToggleEurojackpot(chatId);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var okResult = result.Result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        await _registeredChatService.Received(1).ToggleEurojackpotAsync(chatId);
    }

    [Test]
    public async Task ToggleEurojackpot_ShouldReturnNotFound_WhenChatDoesNotExist()
    {
        // Arrange
        const long chatId = 999;
        _registeredChatService.ToggleEurojackpotAsync(chatId).Returns(Task.FromResult(false));

        // Act
        var result = await _controller.ToggleEurojackpot(chatId);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task ToggleMonthlyReport_ShouldToggleFlag_AndReturnSuccess()
    {
        // Arrange
        const long chatId = 100;
        _registeredChatService.ToggleMonthlyChargingReportAsync(chatId).Returns(Task.FromResult(true));
        var chat = new RegisteredChat
        {
            Id = 1,
            Username = "user1",
            ChatId = chatId,
            MonthlyChargingReportEnabled = true
        };
        _registeredChatService.GetRegisteredChat(chatId).Returns(chat);

        // Act
        var result = await _controller.ToggleMonthlyReport(chatId);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        await _registeredChatService.Received(1).ToggleMonthlyChargingReportAsync(chatId);
    }

    [Test]
    public async Task ToggleDeviceNotifications_ShouldToggleFlag_AndReturnSuccess()
    {
        // Arrange
        const long chatId = 100;
        _registeredChatService.ToggleDeviceNotificationsAsync(chatId).Returns(Task.FromResult(true));
        var chat = new RegisteredChat
        {
            Id = 1,
            Username = "user1",
            ChatId = chatId,
            DeviceNotificationsEnabled = false
        };
        _registeredChatService.GetRegisteredChat(chatId).Returns(chat);

        // Act
        var result = await _controller.ToggleDeviceNotifications(chatId);

        // Assert
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        await _registeredChatService.Received(1).ToggleDeviceNotificationsAsync(chatId);
    }
}
