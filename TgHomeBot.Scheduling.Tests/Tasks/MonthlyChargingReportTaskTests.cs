using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using TgHomeBot.Charging.Contract.Models;
using TgHomeBot.Charging.Contract.Requests;
using TgHomeBot.Charging.Contract.Services;
using TgHomeBot.Common.Contract;
using TgHomeBot.Notifications.Contract;
using TgHomeBot.Notifications.Contract.Requests;
using TgHomeBot.Scheduling.Tasks;

namespace TgHomeBot.Scheduling.Tests.Tasks;

[TestFixture]
public class MonthlyChargingReportTaskTests
{
    private ILogger<MonthlyChargingReportTask> _logger = null!;
    private INotificationConnector _notificationConnector = null!;
    private IMediator _mediator = null!;
    private IMonthlyReportFormatter _formatter = null!;
    private IMonthlyReportPdfGenerator _pdfGenerator = null!;
    private IOptions<FileStorageOptions> _fileStorageOptions = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<MonthlyChargingReportTask>>();
        _notificationConnector = Substitute.For<INotificationConnector>();
        _mediator = Substitute.For<IMediator>();
        _formatter = Substitute.For<IMonthlyReportFormatter>();
        _pdfGenerator = Substitute.For<IMonthlyReportPdfGenerator>();
        _fileStorageOptions = Options.Create(new FileStorageOptions { Path = "temp" });
    }

    [Test]
    public async Task ExecuteAsync_ShouldSendAuthenticationReminder_WhenChargingSessionsFetchFailsDueToMissingAuthentication()
    {
        // Arrange
        _mediator
            .Send(Arg.Any<GetChargingSessionsRequest>(), Arg.Any<CancellationToken>())
            .Returns(ChargingResult<IReadOnlyList<ChargingSession>>.Error("Nicht mit Easee API authentifiziert. Bitte anmelden."));

        var task = new MonthlyChargingReportTask(
            _logger,
            _notificationConnector,
            _mediator,
            _formatter,
            _pdfGenerator,
            _fileStorageOptions);

        // Act
        await task.ExecuteAsync(CancellationToken.None);

        // Assert
        await _notificationConnector.Received(1).SendAsync(
            Arg.Is<string>(message => message.Contains("Bitte bei Easee authentifizieren")),
            NotificationType.General);

        await _notificationConnector.DidNotReceive().SendWithFilesAsync(
            Arg.Any<string>(),
            Arg.Any<IReadOnlyList<FileAttachment>>(),
            Arg.Any<NotificationType>());
    }
}
