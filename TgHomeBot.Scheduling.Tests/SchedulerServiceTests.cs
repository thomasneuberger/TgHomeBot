using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TgHomeBot.Common.Contract;
using TgHomeBot.Scheduling;
using TgHomeBot.Scheduling.Contract;

namespace TgHomeBot.Scheduling.Tests;

[TestFixture]
public class SchedulerServiceTests
{
    private string _testDirectory = null!;
    private ILogger<SchedulerService> _logger = null!;
    private IOptions<FileStorageOptions> _fileStorageOptions = null!;
    private ServiceProvider _serviceProvider = null!;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"TgHomeBotTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _logger = Substitute.For<ILogger<SchedulerService>>();
        _fileStorageOptions = Options.Create(new FileStorageOptions { Path = _testDirectory });

        var services = new ServiceCollection();
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown()
    {
        _serviceProvider.Dispose();
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SchedulerService(null!, _serviceProvider, _fileStorageOptions));
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenServiceProviderIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SchedulerService(_logger, null!, _fileStorageOptions));
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenOptionsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SchedulerService(_logger, _serviceProvider, null!));
    }

    [Test]
    public async Task StartAsync_ShouldComplete_WhenNoConfigurationFilesExist()
    {
        // Arrange
        var service = new SchedulerService(_logger, _serviceProvider, _fileStorageOptions);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await service.StartAsync(CancellationToken.None));
    }

    [Test]
    public void GetScheduledTasks_ShouldReturnEmptyList_WhenNoTasksConfigured()
    {
        // Arrange
        var service = new SchedulerService(_logger, _serviceProvider, _fileStorageOptions);

        // Act
        var result = service.GetScheduledTasks();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetScheduledTasks_ShouldReturnTasks_WhenConfigurationFilesExist()
    {
        // Arrange
        var scheduledTasksPath = Path.Combine(_testDirectory, "ScheduledTasks");
        Directory.CreateDirectory(scheduledTasksPath);
        
        var configContent = @"{
            ""TaskType"": ""TestTask"",
            ""CronExpression"": ""0 0 * * *"",
            ""Enabled"": true
        }";
        File.WriteAllText(Path.Combine(scheduledTasksPath, "test-task.json"), configContent);

        var service = new SchedulerService(_logger, _serviceProvider, _fileStorageOptions);
        await service.StartAsync(CancellationToken.None);

        // Act
        var result = service.GetScheduledTasks();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Has.Count.EqualTo(1));
        var task = result.First();
        Assert.That(task.TaskType, Is.EqualTo("TestTask"));
        Assert.That(task.CronExpression, Is.EqualTo("0 0 * * *"));
        Assert.That(task.Enabled, Is.True);
    }

    [Test]
    public async Task GetScheduledTasks_ShouldIncludeDisabledTasks()
    {
        // Arrange
        var scheduledTasksPath = Path.Combine(_testDirectory, "ScheduledTasks");
        Directory.CreateDirectory(scheduledTasksPath);
        
        var configContent = @"{
            ""TaskType"": ""DisabledTask"",
            ""CronExpression"": ""0 0 * * *"",
            ""Enabled"": false
        }";
        File.WriteAllText(Path.Combine(scheduledTasksPath, "disabled-task.json"), configContent);

        var service = new SchedulerService(_logger, _serviceProvider, _fileStorageOptions);
        await service.StartAsync(CancellationToken.None);

        // Act
        var result = service.GetScheduledTasks();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        var task = result.First();
        Assert.That(task.Enabled, Is.False);
    }

    [Test]
    public async Task RunTaskNowAsync_ShouldReturnFalse_WhenTaskTypeDoesNotExist()
    {
        // Arrange
        var service = new SchedulerService(_logger, _serviceProvider, _fileStorageOptions);

        // Act
        var result = await service.RunTaskNowAsync("NonExistentTask");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task StopAsync_ShouldComplete_WhenCalled()
    {
        // Arrange
        var service = new SchedulerService(_logger, _serviceProvider, _fileStorageOptions);
        await service.StartAsync(CancellationToken.None);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await service.StopAsync(CancellationToken.None));
    }

    [Test]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var service = new SchedulerService(_logger, _serviceProvider, _fileStorageOptions);

        // Act & Assert
        Assert.DoesNotThrow(() => service.Dispose());
    }

    [Test]
    public async Task StartAsync_ShouldHandleInvalidJsonConfiguration()
    {
        // Arrange
        var scheduledTasksPath = Path.Combine(_testDirectory, "ScheduledTasks");
        Directory.CreateDirectory(scheduledTasksPath);
        
        File.WriteAllText(Path.Combine(scheduledTasksPath, "invalid.json"), "{ invalid json");

        var service = new SchedulerService(_logger, _serviceProvider, _fileStorageOptions);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await service.StartAsync(CancellationToken.None));
    }

    [Test]
    public void GetScheduledTasks_ShouldHandleMultipleConfigurationFiles()
    {
        // Arrange
        var scheduledTasksPath = Path.Combine(_testDirectory, "ScheduledTasks");
        Directory.CreateDirectory(scheduledTasksPath);
        
        var config1 = @"{
            ""TaskType"": ""Task1"",
            ""CronExpression"": ""0 0 * * *"",
            ""Enabled"": true
        }";
        var config2 = @"{
            ""TaskType"": ""Task2"",
            ""CronExpression"": ""0 12 * * *"",
            ""Enabled"": false
        }";
        File.WriteAllText(Path.Combine(scheduledTasksPath, "task1.json"), config1);
        File.WriteAllText(Path.Combine(scheduledTasksPath, "task2.json"), config2);

        var service = new SchedulerService(_logger, _serviceProvider, _fileStorageOptions);

        // Act
        var result = service.GetScheduledTasks();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Any(t => t.TaskType == "Task1"), Is.True);
        Assert.That(result.Any(t => t.TaskType == "Task2"), Is.True);
    }

    [Test]
    public async Task StartAsync_ShouldNotStartDisabledTasks()
    {
        // Arrange
        var scheduledTasksPath = Path.Combine(_testDirectory, "ScheduledTasks");
        Directory.CreateDirectory(scheduledTasksPath);
        
        var configContent = @"{
            ""TaskType"": ""DisabledTask"",
            ""CronExpression"": ""0 0 * * *"",
            ""Enabled"": false
        }";
        File.WriteAllText(Path.Combine(scheduledTasksPath, "disabled-task.json"), configContent);

        var service = new SchedulerService(_logger, _serviceProvider, _fileStorageOptions);

        // Act
        await service.StartAsync(CancellationToken.None);

        // Assert - verify task was not started by checking that NextExecutionTime is null
        var tasks = service.GetScheduledTasks();
        Assert.That(tasks, Has.Count.EqualTo(1));
        Assert.That(tasks.First().NextExecutionTime, Is.Null);
    }
}
