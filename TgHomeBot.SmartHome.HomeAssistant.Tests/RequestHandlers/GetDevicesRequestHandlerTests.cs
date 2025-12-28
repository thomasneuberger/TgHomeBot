using NSubstitute;
using NUnit.Framework;
using TgHomeBot.SmartHome.Contract;
using TgHomeBot.SmartHome.Contract.Models;
using TgHomeBot.SmartHome.Contract.Requests;
using TgHomeBot.SmartHome.HomeAssistant.RequestHandlers;

namespace TgHomeBot.SmartHome.HomeAssistant.Tests.RequestHandlers;

[TestFixture]
public class GetDevicesRequestHandlerTests
{
    private ISmartHomeConnector _connector = null!;
    private GetDevicesRequestHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _connector = Substitute.For<ISmartHomeConnector>();
        _handler = new GetDevicesRequestHandler(_connector);
    }

    [Test]
    public async Task Handle_ShouldCallGetDevicesWithoutParameters_WhenDevicesIsNull()
    {
        // Arrange
        var request = new GetDevicesRequest(null);
        var expectedDevices = new List<SmartDevice>
        {
            new() { Id = "device1", Name = "Device 1", State = "on" }
        }.AsReadOnly();
        _connector.GetDevices().Returns(Task.FromResult<IReadOnlyList<SmartDevice>>(expectedDevices));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo("device1"));
        await _connector.Received(1).GetDevices();
    }

    [Test]
    public async Task Handle_ShouldCallGetDevicesWithParameters_WhenDevicesIsProvided()
    {
        // Arrange
        var monitoredDevices = new List<MonitoredDevice>
        {
            new() { Id = "sensor.device1", Name = "Device 1" }
        }.AsReadOnly();
        var request = new GetDevicesRequest(monitoredDevices);
        var expectedDevices = new List<SmartDevice>
        {
            new() { Id = "sensor.device1", Name = "Device 1", State = "50" }
        }.AsReadOnly();
        _connector.GetDevices(monitoredDevices).Returns(Task.FromResult<IReadOnlyList<SmartDevice>>(expectedDevices));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result[0].Id, Is.EqualTo("sensor.device1"));
        await _connector.Received(1).GetDevices(monitoredDevices);
    }

    [Test]
    public async Task Handle_ShouldReturnEmptyList_WhenNoDevicesFound()
    {
        // Arrange
        var request = new GetDevicesRequest(null);
        var emptyDevices = new List<SmartDevice>().AsReadOnly();
        _connector.GetDevices().Returns(Task.FromResult<IReadOnlyList<SmartDevice>>(emptyDevices));

        // Act
        var result = await _handler.Handle(request, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }
}
