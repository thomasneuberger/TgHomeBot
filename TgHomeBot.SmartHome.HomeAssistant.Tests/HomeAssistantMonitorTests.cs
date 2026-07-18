using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TgHomeBot.SmartHome.Contract;
using TgHomeBot.SmartHome.Contract.Models;

namespace TgHomeBot.SmartHome.HomeAssistant.Tests;

[TestFixture]
public class HomeAssistantMonitorTests
{
    private ISmartHomeConnector _connector = null!;
    private IServiceProvider _serviceProvider = null!;
    private HomeAssistantMonitor _monitor = null!;

    [SetUp]
    public void SetUp()
    {
        _connector = Substitute.For<ISmartHomeConnector>();

        var services = new ServiceCollection();
        services.AddSingleton(_connector);
        _serviceProvider = services.BuildServiceProvider();

        var options = Options.Create(new HomeAssistantOptions
        {
            BaseUrl = "http://localhost:8123",
            Token = "test-token"
        });

        _monitor = new HomeAssistantMonitor(
            [],
            options,
            _serviceProvider,
            Substitute.For<ILogger<HomeAssistantMonitor>>());
    }

    [Test]
    public async Task IsConditionMetAsync_WhenNoConditionDeviceConfigured_ReturnsTrue()
    {
        // Arrange
        var device = new MonitoredDevice { Id = "switch.washer", Name = "Washer" };

        // Act
        var result = await _monitor.IsConditionMetAsync(device, _serviceProvider);

        // Assert
        Assert.That(result, Is.True);
        await _connector.DidNotReceive().GetDevice(Arg.Any<string>());
    }

    [Test]
    public async Task IsConditionMetAsync_WhenConditionDeviceIsOn_ReturnsTrue()
    {
        // Arrange
        var device = new MonitoredDevice
        {
            Id = "switch.washer",
            Name = "Washer",
            ConditionDeviceId = "binary_sensor.someone_home"
        };
        _connector.GetDevice("binary_sensor.someone_home")
            .Returns(Task.FromResult<SmartDevice?>(new SmartDevice { Id = "binary_sensor.someone_home", Name = "Someone home", State = "on" }));

        // Act
        var result = await _monitor.IsConditionMetAsync(device, _serviceProvider);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsConditionMetAsync_WhenConditionDeviceIsOff_ReturnsFalse()
    {
        // Arrange
        var device = new MonitoredDevice
        {
            Id = "switch.washer",
            Name = "Washer",
            ConditionDeviceId = "binary_sensor.someone_home"
        };
        _connector.GetDevice("binary_sensor.someone_home")
            .Returns(Task.FromResult<SmartDevice?>(new SmartDevice { Id = "binary_sensor.someone_home", Name = "Someone home", State = "off" }));

        // Act
        var result = await _monitor.IsConditionMetAsync(device, _serviceProvider);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsConditionMetAsync_WhenConditionDeviceIsOnCaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var device = new MonitoredDevice
        {
            Id = "switch.washer",
            Name = "Washer",
            ConditionDeviceId = "binary_sensor.someone_home"
        };
        _connector.GetDevice("binary_sensor.someone_home")
            .Returns(Task.FromResult<SmartDevice?>(new SmartDevice { Id = "binary_sensor.someone_home", Name = "Someone home", State = "ON" }));

        // Act
        var result = await _monitor.IsConditionMetAsync(device, _serviceProvider);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsConditionMetAsync_WhenConditionDeviceNotFound_ReturnsFalse()
    {
        // Arrange
        var device = new MonitoredDevice
        {
            Id = "switch.washer",
            Name = "Washer",
            ConditionDeviceId = "binary_sensor.someone_home"
        };
        _connector.GetDevice("binary_sensor.someone_home")
            .Returns(Task.FromResult<SmartDevice?>(null));

        // Act
        var result = await _monitor.IsConditionMetAsync(device, _serviceProvider);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsConditionMetAsync_QueriesConditionDeviceWithCorrectId()
    {
        // Arrange
        var conditionDeviceId = "binary_sensor.someone_home";
        var device = new MonitoredDevice
        {
            Id = "switch.washer",
            Name = "Washer",
            ConditionDeviceId = conditionDeviceId
        };
        _connector.GetDevice(conditionDeviceId)
            .Returns(Task.FromResult<SmartDevice?>(null));

        // Act
        await _monitor.IsConditionMetAsync(device, _serviceProvider);

        // Assert
        await _connector.Received(1).GetDevice(conditionDeviceId);
    }
}
