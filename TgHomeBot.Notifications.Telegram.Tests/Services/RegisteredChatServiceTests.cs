using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TgHomeBot.Common.Contract;
using TgHomeBot.Notifications.Telegram.Models;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram.Tests.Services;

[TestFixture]
public class RegisteredChatServiceTests
{
    private string _testDirectory = null!;
    private ILogger<RegisteredChatService> _logger = null!;
    private IOptions<FileStorageOptions> _fileStorageOptions = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"TgHomeBotTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _logger = Substitute.For<ILogger<RegisteredChatService>>();
        _fileStorageOptions = Options.Create(new FileStorageOptions { Path = _testDirectory });
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Test]
    public void RegisteredChats_ShouldReturnEmptyList_WhenNoChatsRegistered()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);

        // Act
        var result = service.RegisteredChats;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task RegisterChat_ShouldAddNewChat_WhenChatDoesNotExist()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);
        const long userId = 12345;
        const string username = "testuser";
        const long chatId = 67890;

        // Act
        var result = await service.RegisterChat(userId, username, chatId);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(service.RegisteredChats, Has.Count.EqualTo(1));
        var registeredChat = service.RegisteredChats[0];
        Assert.That(registeredChat.Id, Is.EqualTo(userId));
        Assert.That(registeredChat.Username, Is.EqualTo(username));
        Assert.That(registeredChat.ChatId, Is.EqualTo(chatId));
        Assert.That(registeredChat.ChatName, Is.Null);
    }

    [Test]
    public async Task RegisterChat_WithChatName_ShouldAddNewChatWithName()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);
        const long userId = 12345;
        const string username = "testuser";
        const long chatId = 67890;
        const string chatName = "Test Chat";

        // Act
        var result = await service.RegisterChat(userId, username, chatId, chatName);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(service.RegisteredChats, Has.Count.EqualTo(1));
        var registeredChat = service.RegisteredChats[0];
        Assert.That(registeredChat.ChatName, Is.EqualTo(chatName));
    }

    [Test]
    public async Task RegisterChat_ShouldReturnFalse_WhenChatAlreadyExists()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);
        const long userId = 12345;
        const string username = "testuser";
        const long chatId = 67890;
        await service.RegisterChat(userId, username, chatId);

        // Act
        var result = await service.RegisterChat(userId, username, chatId);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(service.RegisteredChats, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task UnregisterChatAsync_ShouldRemoveChat_WhenChatExists()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);
        const long userId = 12345;
        const string username = "testuser";
        const long chatId = 67890;
        await service.RegisterChat(userId, username, chatId);

        // Act
        var result = await service.UnregisterChatAsync(chatId);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(service.RegisteredChats, Is.Empty);
    }

    [Test]
    public async Task UnregisterChatAsync_ShouldReturnFalse_WhenChatDoesNotExist()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);
        const long chatId = 67890;

        // Act
        var result = await service.UnregisterChatAsync(chatId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetRegisteredChat_ShouldReturnChat_WhenChatExists()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);
        const long userId = 12345;
        const string username = "testuser";
        const long chatId = 67890;
        await service.RegisterChat(userId, username, chatId);

        // Act
        var result = service.GetRegisteredChat(chatId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ChatId, Is.EqualTo(chatId));
        Assert.That(result.Username, Is.EqualTo(username));
    }

    [Test]
    public void GetRegisteredChat_ShouldReturnNull_WhenChatDoesNotExist()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);
        const long chatId = 67890;

        // Act
        var result = service.GetRegisteredChat(chatId);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ToggleEurojackpotAsync_ShouldToggleFlag_WhenChatExists()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);
        const long userId = 12345;
        const string username = "testuser";
        const long chatId = 67890;
        await service.RegisterChat(userId, username, chatId);
        var initialState = service.GetRegisteredChat(chatId)!.EurojackpotEnabled;

        // Act
        var result = await service.ToggleEurojackpotAsync(chatId);

        // Assert
        Assert.That(result, Is.True);
        var chat = service.GetRegisteredChat(chatId);
        Assert.That(chat!.EurojackpotEnabled, Is.EqualTo(!initialState));
    }

    [Test]
    public async Task ToggleEurojackpotAsync_ShouldReturnFalse_WhenChatDoesNotExist()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);
        const long chatId = 67890;

        // Act
        var result = await service.ToggleEurojackpotAsync(chatId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ToggleMonthlyChargingReportAsync_ShouldToggleFlag_WhenChatExists()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);
        const long userId = 12345;
        const string username = "testuser";
        const long chatId = 67890;
        await service.RegisterChat(userId, username, chatId);
        var initialState = service.GetRegisteredChat(chatId)!.MonthlyChargingReportEnabled;

        // Act
        var result = await service.ToggleMonthlyChargingReportAsync(chatId);

        // Assert
        Assert.That(result, Is.True);
        var chat = service.GetRegisteredChat(chatId);
        Assert.That(chat!.MonthlyChargingReportEnabled, Is.EqualTo(!initialState));
    }

    [Test]
    public async Task ToggleMonthlyChargingReportAsync_ShouldReturnFalse_WhenChatDoesNotExist()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);
        const long chatId = 67890;

        // Act
        var result = await service.ToggleMonthlyChargingReportAsync(chatId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ToggleDeviceNotificationsAsync_ShouldToggleFlag_WhenChatExists()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);
        const long userId = 12345;
        const string username = "testuser";
        const long chatId = 67890;
        await service.RegisterChat(userId, username, chatId);
        var initialState = service.GetRegisteredChat(chatId)!.DeviceNotificationsEnabled;

        // Act
        var result = await service.ToggleDeviceNotificationsAsync(chatId);

        // Assert
        Assert.That(result, Is.True);
        var chat = service.GetRegisteredChat(chatId);
        Assert.That(chat!.DeviceNotificationsEnabled, Is.EqualTo(!initialState));
    }

    [Test]
    public async Task ToggleDeviceNotificationsAsync_ShouldReturnFalse_WhenChatDoesNotExist()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);
        const long chatId = 67890;

        // Act
        var result = await service.ToggleDeviceNotificationsAsync(chatId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task Clear_ShouldRemoveAllChats()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);
        await service.RegisterChat(12345, "user1", 11111);
        await service.RegisterChat(23456, "user2", 22222);

        // Act
        service.Clear();

        // Assert
        Assert.That(service.RegisteredChats, Is.Empty);
    }

    [Test]
    public async Task LoadRegisteredChats_ShouldLoadFromFile_WhenFileExists()
    {
        // Arrange
        var service1 = new RegisteredChatService(_fileStorageOptions, _logger);
        await service1.RegisterChat(12345, "user1", 11111);
        await service1.RegisterChat(23456, "user2", 22222);

        // Act
        var service2 = new RegisteredChatService(_fileStorageOptions, _logger);
        await service2.LoadRegisteredChats();

        // Assert
        Assert.That(service2.RegisteredChats, Has.Count.EqualTo(2));
        Assert.That(service2.RegisteredChats.Any(c => c.Username == "user1"), Is.True);
        Assert.That(service2.RegisteredChats.Any(c => c.Username == "user2"), Is.True);
    }

    [Test]
    public async Task LoadRegisteredChats_ShouldNotFail_WhenFileDoesNotExist()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await service.LoadRegisteredChats());
        Assert.That(service.RegisteredChats, Is.Empty);
    }

    [Test]
    public async Task SaveChangesAsync_ShouldPersistChanges()
    {
        // Arrange
        var service1 = new RegisteredChatService(_fileStorageOptions, _logger);
        await service1.RegisterChat(12345, "user1", 11111);
        var chat = service1.GetRegisteredChat(11111)!;
        chat.EurojackpotEnabled = false;
        
        // Act
        await service1.SaveChangesAsync();

        // Assert
        var service2 = new RegisteredChatService(_fileStorageOptions, _logger);
        await service2.LoadRegisteredChats();
        var loadedChat = service2.GetRegisteredChat(11111);
        Assert.That(loadedChat, Is.Not.Null);
        Assert.That(loadedChat!.EurojackpotEnabled, Is.False);
    }

    [Test]
    public async Task RegisteredChats_ShouldHaveDefaultFlagsEnabled()
    {
        // Arrange
        var service = new RegisteredChatService(_fileStorageOptions, _logger);

        // Act
        await service.RegisterChat(12345, "testuser", 67890);
        var chat = service.GetRegisteredChat(67890);

        // Assert
        Assert.That(chat, Is.Not.Null);
        Assert.That(chat!.EurojackpotEnabled, Is.True);
        Assert.That(chat.MonthlyChargingReportEnabled, Is.True);
        Assert.That(chat.DeviceNotificationsEnabled, Is.True);
    }
}
