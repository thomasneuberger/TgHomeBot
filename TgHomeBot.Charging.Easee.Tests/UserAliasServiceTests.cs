using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TgHomeBot.Charging.Contract.Models;
using TgHomeBot.Charging.Easee;
using TgHomeBot.Common.Contract;

namespace TgHomeBot.Charging.Easee.Tests;

[TestFixture]
public class UserAliasServiceTests
{
    private string _testDirectory = null!;
    private ILogger<UserAliasService> _logger = null!;
    private IOptions<FileStorageOptions> _fileStorageOptions = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"TgHomeBotTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _logger = Substitute.For<ILogger<UserAliasService>>();
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
    public void GetAllAliases_ShouldReturnEmptyList_WhenNoAliasesExist()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);

        // Act
        var result = service.GetAllAliases();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SaveAlias_ShouldAddNewAlias_WhenAliasDoesNotExist()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);
        var userAlias = new UserAlias
        {
            UserId = "user123",
            Alias = "John Doe"
        };

        // Act
        service.SaveAlias(userAlias);

        // Assert
        var aliases = service.GetAllAliases();
        Assert.That(aliases, Has.Count.EqualTo(1));
        Assert.That(aliases[0].UserId, Is.EqualTo("user123"));
        Assert.That(aliases[0].Alias, Is.EqualTo("John Doe"));
    }

    [Test]
    public void SaveAlias_ShouldUpdateExistingAlias_WhenAliasExists()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);
        var userAlias = new UserAlias
        {
            UserId = "user123",
            Alias = "John Doe"
        };
        service.SaveAlias(userAlias);

        var updatedAlias = new UserAlias
        {
            UserId = "user123",
            Alias = "Jane Doe",
            TokenIds = new List<string> { "token1" }
        };

        // Act
        service.SaveAlias(updatedAlias);

        // Assert
        var aliases = service.GetAllAliases();
        Assert.That(aliases, Has.Count.EqualTo(1));
        Assert.That(aliases[0].Alias, Is.EqualTo("Jane Doe"));
        Assert.That(aliases[0].TokenIds, Does.Contain("token1"));
    }

    [Test]
    public void SaveAlias_ShouldThrowArgumentNullException_WhenAliasIsNull()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.SaveAlias(null!));
    }

    [Test]
    public void GetAliasByUserId_ShouldReturnAlias_WhenAliasExists()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);
        var userAlias = new UserAlias
        {
            UserId = "user123",
            Alias = "John Doe"
        };
        service.SaveAlias(userAlias);

        // Act
        var result = service.GetAliasByUserId("user123");

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.UserId, Is.EqualTo("user123"));
        Assert.That(result.Alias, Is.EqualTo("John Doe"));
    }

    [Test]
    public void GetAliasByUserId_ShouldReturnNull_WhenAliasDoesNotExist()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);

        // Act
        var result = service.GetAliasByUserId("nonexistent");

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void DeleteAlias_ShouldRemoveAlias_WhenAliasExists()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);
        var userAlias = new UserAlias
        {
            UserId = "user123",
            Alias = "John Doe"
        };
        service.SaveAlias(userAlias);

        // Act
        service.DeleteAlias("user123");

        // Assert
        var aliases = service.GetAllAliases();
        Assert.That(aliases, Is.Empty);
    }

    [Test]
    public void DeleteAlias_ShouldDoNothing_WhenAliasDoesNotExist()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);

        // Act & Assert
        Assert.DoesNotThrow(() => service.DeleteAlias("nonexistent"));
    }

    [Test]
    public void ResolveUserName_ShouldReturnAlias_WhenAliasExistsByUserId()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);
        var userAlias = new UserAlias
        {
            UserId = "user123",
            Alias = "John Doe"
        };
        service.SaveAlias(userAlias);

        // Act
        var result = service.ResolveUserName("user123");

        // Assert
        Assert.That(result, Is.EqualTo("John Doe"));
    }

    [Test]
    public void ResolveUserName_ShouldReturnUserId_WhenAliasDoesNotExist()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);

        // Act
        var result = service.ResolveUserName("user123");

        // Assert
        Assert.That(result, Is.EqualTo("user123"));
    }

    [Test]
    public void ResolveUserName_ShouldReturnAlias_WhenMatchedByToken()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);
        var userAlias = new UserAlias
        {
            UserId = "user123",
            Alias = "John Doe",
            TokenIds = new List<string> { "token1", "token2" }
        };
        service.SaveAlias(userAlias);

        // Act
        var result = service.ResolveUserName("otheruser", "token1");

        // Assert
        Assert.That(result, Is.EqualTo("John Doe"));
    }

    [Test]
    public void ResolveUserName_ShouldReturnUserId_WhenTokenDoesNotMatch()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);
        var userAlias = new UserAlias
        {
            UserId = "user123",
            Alias = "John Doe",
            TokenIds = new List<string> { "token1", "token2" }
        };
        service.SaveAlias(userAlias);

        // Act
        var result = service.ResolveUserName("otheruser", "token3");

        // Assert
        Assert.That(result, Is.EqualTo("otheruser"));
    }

    [Test]
    public void TrackUserId_ShouldAddNewUserId_WhenNotTracked()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);

        // Act
        service.TrackUserId("user123");

        // Assert
        var trackedUserIds = service.GetTrackedUserIds();
        Assert.That(trackedUserIds, Does.Contain("user123"));
    }

    [Test]
    public void TrackUserId_ShouldNotDuplicate_WhenUserIdAlreadyTracked()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);
        service.TrackUserId("user123");

        // Act
        service.TrackUserId("user123");

        // Assert
        var trackedUserIds = service.GetTrackedUserIds();
        Assert.That(trackedUserIds, Has.Count.EqualTo(1));
        Assert.That(trackedUserIds, Does.Contain("user123"));
    }

    [Test]
    public void GetTrackedUserIds_ShouldReturnSortedList()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);
        service.TrackUserId("user3");
        service.TrackUserId("user1");
        service.TrackUserId("user2");

        // Act
        var result = service.GetTrackedUserIds();

        // Assert
        Assert.That(result, Is.EqualTo(new[] { "user1", "user2", "user3" }));
    }

    [Test]
    public void GetTrackedUserIds_ShouldReturnEmptyList_WhenNoUserIdsTracked()
    {
        // Arrange
        var service = new UserAliasService(_logger, _fileStorageOptions);

        // Act
        var result = service.GetTrackedUserIds();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SaveAlias_ShouldPersistToFile()
    {
        // Arrange
        var service1 = new UserAliasService(_logger, _fileStorageOptions);
        var userAlias = new UserAlias
        {
            UserId = "user123",
            Alias = "John Doe",
            TokenIds = new List<string> { "token1" }
        };
        service1.SaveAlias(userAlias);

        // Act
        var service2 = new UserAliasService(_logger, _fileStorageOptions);

        // Assert
        var aliases = service2.GetAllAliases();
        Assert.That(aliases, Has.Count.EqualTo(1));
        Assert.That(aliases[0].UserId, Is.EqualTo("user123"));
        Assert.That(aliases[0].Alias, Is.EqualTo("John Doe"));
        Assert.That(aliases[0].TokenIds, Does.Contain("token1"));
    }

    [Test]
    public void TrackUserId_ShouldPersistToFile()
    {
        // Arrange
        var service1 = new UserAliasService(_logger, _fileStorageOptions);
        service1.TrackUserId("user123");
        service1.TrackUserId("user456");

        // Act
        var service2 = new UserAliasService(_logger, _fileStorageOptions);

        // Assert
        var trackedUserIds = service2.GetTrackedUserIds();
        Assert.That(trackedUserIds, Does.Contain("user123"));
        Assert.That(trackedUserIds, Does.Contain("user456"));
    }
}
