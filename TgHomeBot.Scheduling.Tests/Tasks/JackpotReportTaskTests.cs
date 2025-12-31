using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using System.Net;
using System.Text;
using TgHomeBot.Notifications.Contract;
using TgHomeBot.Scheduling.Tasks;

namespace TgHomeBot.Scheduling.Tests.Tasks;

[TestFixture]
public class JackpotReportTaskTests
{
    private ILogger<JackpotReportTask> _logger = null!;
    private INotificationConnector _notificationConnector = null!;
    private IHttpClientFactory _httpClientFactory = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = Substitute.For<ILogger<JackpotReportTask>>();
        _notificationConnector = Substitute.For<INotificationConnector>();
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
    }

    [Test]
    public async Task ExecuteAsync_ShouldFormatJackpotCorrectly_WhenApiReturnsMillionEuros()
    {
        // Arrange
        var apiResponse = @"{
            ""last"": {
                ""date"": {
                    ""full"": ""2024-01-15""
                },
                ""numbers"": [5, 12, 23, 34, 45],
                ""euroNumbers"": [3, 7],
                ""jackpot"": 10
            },
            ""next"": {
                ""date"": {
                    ""full"": ""2024-01-19""
                },
                ""numbers"": [],
                ""euroNumbers"": [],
                ""jackpot"": 15
            }
        }";

        var httpMessageHandler = new MockHttpMessageHandler(apiResponse, HttpStatusCode.OK);
        var httpClient = new HttpClient(httpMessageHandler);
        _httpClientFactory.CreateClient().Returns(httpClient);

        var task = new JackpotReportTask(_logger, _notificationConnector, _httpClientFactory);

        // Act
        await task.ExecuteAsync(CancellationToken.None);

        // Assert
        await _notificationConnector.Received(1).SendAsync(
            Arg.Is<string>(msg => 
                msg.Contains("10.000.000 €") && // 10 million euros
                msg.Contains("15.000.000 €")),  // 15 million euros
            NotificationType.Eurojackpot);
    }

    [Test]
    public async Task ExecuteAsync_ShouldHandleSmallJackpots()
    {
        // Arrange - jackpot of 1 million euros (1 in the API)
        var apiResponse = @"{
            ""last"": {
                ""date"": {
                    ""full"": ""2024-01-15""
                },
                ""numbers"": [5, 12, 23, 34, 45],
                ""euroNumbers"": [3, 7],
                ""jackpot"": 1
            }
        }";

        var httpMessageHandler = new MockHttpMessageHandler(apiResponse, HttpStatusCode.OK);
        var httpClient = new HttpClient(httpMessageHandler);
        _httpClientFactory.CreateClient().Returns(httpClient);

        var task = new JackpotReportTask(_logger, _notificationConnector, _httpClientFactory);

        // Act
        await task.ExecuteAsync(CancellationToken.None);

        // Assert
        await _notificationConnector.Received(1).SendAsync(
            Arg.Is<string>(msg => msg.Contains("1.000.000 €")),
            NotificationType.Eurojackpot);
    }

    [Test]
    public async Task ExecuteAsync_ShouldHandleLargeJackpots()
    {
        // Arrange - jackpot of 120 million euros (120 in the API)
        var apiResponse = @"{
            ""last"": {
                ""date"": {
                    ""full"": ""2024-01-15""
                },
                ""numbers"": [5, 12, 23, 34, 45],
                ""euroNumbers"": [3, 7],
                ""jackpot"": 120
            }
        }";

        var httpMessageHandler = new MockHttpMessageHandler(apiResponse, HttpStatusCode.OK);
        var httpClient = new HttpClient(httpMessageHandler);
        _httpClientFactory.CreateClient().Returns(httpClient);

        var task = new JackpotReportTask(_logger, _notificationConnector, _httpClientFactory);

        // Act
        await task.ExecuteAsync(CancellationToken.None);

        // Assert
        await _notificationConnector.Received(1).SendAsync(
            Arg.Is<string>(msg => msg.Contains("120.000.000 €")),
            NotificationType.Eurojackpot);
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new JackpotReportTask(null!, _notificationConnector, _httpClientFactory));
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenNotificationConnectorIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new JackpotReportTask(_logger, null!, _httpClientFactory));
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenHttpClientFactoryIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new JackpotReportTask(_logger, _notificationConnector, null!));
    }

    [Test]
    public async Task ExecuteAsync_ShouldNotSendNotification_WhenApiReturnsError()
    {
        // Arrange
        var httpMessageHandler = new MockHttpMessageHandler("", HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(httpMessageHandler);
        _httpClientFactory.CreateClient().Returns(httpClient);

        var task = new JackpotReportTask(_logger, _notificationConnector, _httpClientFactory);

        // Act
        await task.ExecuteAsync(CancellationToken.None);

        // Assert
        await _notificationConnector.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<NotificationType>());
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _response;
        private readonly HttpStatusCode _statusCode;

        public MockHttpMessageHandler(string response, HttpStatusCode statusCode)
        {
            _response = response;
            _statusCode = statusCode;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = _statusCode,
                Content = new StringContent(_response, Encoding.UTF8, "application/json")
            });
        }
    }
}
