using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TgHomeBot.SmartHome.Contract;
using TgHomeBot.SmartHome.Contract.Models;
using TgHomeBot.SmartHome.HomeAssistant.Models;

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

    // --- IsConditionMetAsync tests ---

    [Test]
    public async Task IsConditionMetAsync_WhenNoConditionDeviceConfigured_ReturnsTrue()
    {
        var device = new MonitoredDevice { Id = "switch.washer", Name = "Washer" };

        var result = await _monitor.IsConditionMetAsync(device, _serviceProvider);

        Assert.That(result, Is.True);
        await _connector.DidNotReceive().GetDevice(Arg.Any<string>());
    }

    [Test]
    public async Task IsConditionMetAsync_WhenConditionDeviceIsOn_ReturnsTrue()
    {
        var device = new MonitoredDevice { Id = "switch.washer", Name = "Washer", ConditionDeviceId = "binary_sensor.someone_home" };
        _connector.GetDevice("binary_sensor.someone_home")
            .Returns(Task.FromResult<SmartDevice?>(new SmartDevice { Id = "binary_sensor.someone_home", Name = "Someone home", State = "on" }));

        var result = await _monitor.IsConditionMetAsync(device, _serviceProvider);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsConditionMetAsync_WhenConditionDeviceIsOff_ReturnsFalse()
    {
        var device = new MonitoredDevice { Id = "switch.washer", Name = "Washer", ConditionDeviceId = "binary_sensor.someone_home" };
        _connector.GetDevice("binary_sensor.someone_home")
            .Returns(Task.FromResult<SmartDevice?>(new SmartDevice { Id = "binary_sensor.someone_home", Name = "Someone home", State = "off" }));

        var result = await _monitor.IsConditionMetAsync(device, _serviceProvider);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsConditionMetAsync_WhenConditionDeviceIsOnCaseInsensitive_ReturnsTrue()
    {
        var device = new MonitoredDevice { Id = "switch.washer", Name = "Washer", ConditionDeviceId = "binary_sensor.someone_home" };
        _connector.GetDevice("binary_sensor.someone_home")
            .Returns(Task.FromResult<SmartDevice?>(new SmartDevice { Id = "binary_sensor.someone_home", Name = "Someone home", State = "ON" }));

        var result = await _monitor.IsConditionMetAsync(device, _serviceProvider);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsConditionMetAsync_WhenConditionDeviceNotFound_ReturnsFalse()
    {
        var device = new MonitoredDevice { Id = "switch.washer", Name = "Washer", ConditionDeviceId = "binary_sensor.someone_home" };
        _connector.GetDevice("binary_sensor.someone_home")
            .Returns(Task.FromResult<SmartDevice?>(null));

        var result = await _monitor.IsConditionMetAsync(device, _serviceProvider);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsConditionMetAsync_QueriesConditionDeviceWithCorrectId()
    {
        var conditionDeviceId = "binary_sensor.someone_home";
        var device = new MonitoredDevice { Id = "switch.washer", Name = "Washer", ConditionDeviceId = conditionDeviceId };
        _connector.GetDevice(conditionDeviceId).Returns(Task.FromResult<SmartDevice?>(null));

        await _monitor.IsConditionMetAsync(device, _serviceProvider);

        await _connector.Received(1).GetDevice(conditionDeviceId);
    }

    // --- GetState tests ---

    [Test]
    public void GetState_WhenValueAboveRunningThreshold_ReturnsRunning()
    {
        var result = HomeAssistantMonitor.GetState(runningThreshold: 10f, offThreshold: 2f, state: "15");

        Assert.That(result, Is.EqualTo(DeviceState.Running));
    }

    [Test]
    public void GetState_WhenValueBelowOffThreshold_ReturnsOff()
    {
        var result = HomeAssistantMonitor.GetState(runningThreshold: 10f, offThreshold: 2f, state: "1");

        Assert.That(result, Is.EqualTo(DeviceState.Off));
    }

    [Test]
    public void GetState_WhenValueBetweenThresholds_ReturnsWaiting()
    {
        var result = HomeAssistantMonitor.GetState(runningThreshold: 10f, offThreshold: 2f, state: "5");

        Assert.That(result, Is.EqualTo(DeviceState.Waiting));
    }

    [Test]
    public void GetState_WhenStateIsNotNumeric_ReturnsUnknown()
    {
        var result = HomeAssistantMonitor.GetState(runningThreshold: 10f, offThreshold: 2f, state: "unavailable");

        Assert.That(result, Is.EqualTo(DeviceState.Unknown));
    }

    // --- AboveThreshold configuration tests ---

    [Test]
    public void DeviceStateThresholds_CanConfigureAboveThresholdAlone()
    {
        var thresholds = new DeviceStateThresholds { AboveThreshold = 5f };

        Assert.That(thresholds.AboveThreshold, Is.EqualTo(5f));
        Assert.That(thresholds.RunningThreshold, Is.Null);
        Assert.That(thresholds.OffThreshold, Is.Null);
    }

    [Test]
    public void DeviceStateThresholds_CanConfigureAllThresholds()
    {
        var thresholds = new DeviceStateThresholds
        {
            RunningThreshold = 10f,
            OffThreshold = 2f,
            AboveThreshold = 5f
        };

        Assert.That(thresholds.RunningThreshold, Is.EqualTo(10f));
        Assert.That(thresholds.OffThreshold, Is.EqualTo(2f));
        Assert.That(thresholds.AboveThreshold, Is.EqualTo(5f));
    }
}
