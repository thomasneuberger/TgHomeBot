using NUnit.Framework;
using TgHomeBot.Charging.Contract.Models;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram.Tests.Services;

[TestFixture]
public class DetailedReportCsvGeneratorTests
{
    private DetailedReportCsvGenerator _generator = null!;

    [SetUp]
    public void SetUp()
    {
        _generator = new DetailedReportCsvGenerator();
    }

    [Test]
    public void GetFileName_ShouldReturnCorrectFormat()
    {
        // Act
        var fileName = _generator.GetFileName(2024, 3);

        // Assert
        Assert.That(fileName, Is.EqualTo("202403-charging.csv"));
    }

    [Test]
    public void GetFileName_ShouldMatchPdfNamingSchema()
    {
        // Act
        var fileName = _generator.GetFileName(2024, 12);

        // Assert
        Assert.That(fileName, Does.StartWith("202412-charging"));
        Assert.That(fileName, Does.EndWith(".csv"));
    }

    [Test]
    public void GetFileName_ShouldSortAlphabeticallyByYearAndMonth()
    {
        // Arrange
        var fileNames = new List<string>
        {
            _generator.GetFileName(2024, 12),
            _generator.GetFileName(2024, 1),
            _generator.GetFileName(2023, 6),
            _generator.GetFileName(2024, 3)
        };

        // Act
        var sorted = fileNames.OrderBy(f => f).ToList();

        // Assert
        Assert.That(sorted[0], Is.EqualTo("202306-charging.csv"));
        Assert.That(sorted[1], Is.EqualTo("202401-charging.csv"));
        Assert.That(sorted[2], Is.EqualTo("202403-charging.csv"));
        Assert.That(sorted[3], Is.EqualTo("202412-charging.csv"));
    }

    [Test]
    public void GenerateMonthlyCsv_ShouldCreateValidCsv()
    {
        // Arrange
        var sessions = new List<ChargingSession>
        {
            new()
            {
                UserId = "user1",
                UserName = "Test User 1",
                CarConnected = new DateTime(2024, 3, 15, 10, 0, 0),
                CarDisconnected = new DateTime(2024, 3, 15, 12, 0, 0),
                KiloWattHours = 25.5,
                ActualDurationSeconds = 7200
            },
            new()
            {
                UserId = "user2",
                UserName = "Test User 2",
                CarConnected = new DateTime(2024, 3, 20, 14, 0, 0),
                CarDisconnected = new DateTime(2024, 3, 20, 16, 0, 0),
                KiloWattHours = 30.2,
                ActualDurationSeconds = 7200
            }
        };

        // Act
        var csvData = _generator.GenerateMonthlyCsv(sessions, 2024, 3);
        var csvText = System.Text.Encoding.UTF8.GetString(csvData);

        // Assert
        Assert.That(csvData, Is.Not.Null);
        Assert.That(csvData.Length, Is.GreaterThan(0));
        Assert.That(csvText, Does.Contain("User,Start,End,Duration (minutes),Energy (kWh)"));
        Assert.That(csvText, Does.Contain("Test User 1"));
        Assert.That(csvText, Does.Contain("Test User 2"));
        Assert.That(csvText, Does.Contain("25.50"));
        Assert.That(csvText, Does.Contain("30.20"));
    }

    [Test]
    public void GenerateMonthlyCsv_ShouldOrderByUserNameThenTimestamp()
    {
        // Arrange
        var sessions = new List<ChargingSession>
        {
            new()
            {
                UserId = "user2",
                UserName = "User B",
                CarConnected = new DateTime(2024, 3, 20, 14, 0, 0),
                KiloWattHours = 30.2
            },
            new()
            {
                UserId = "user1",
                UserName = "User A",
                CarConnected = new DateTime(2024, 3, 15, 10, 0, 0),
                KiloWattHours = 25.5
            },
            new()
            {
                UserId = "user1",
                UserName = "User A",
                CarConnected = new DateTime(2024, 3, 10, 8, 0, 0),
                KiloWattHours = 20.0
            }
        };

        // Act
        var csvData = _generator.GenerateMonthlyCsv(sessions, 2024, 3);
        var csvText = System.Text.Encoding.UTF8.GetString(csvData);
        var lines = csvText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Assert
        Assert.That(lines.Length, Is.EqualTo(4)); // Header + 3 sessions
        Assert.That(lines[1], Does.Contain("User A")); // First session by User A
        Assert.That(lines[1], Does.Contain("2024-03-10")); // Earliest timestamp for User A
        Assert.That(lines[2], Does.Contain("User A")); // Second session by User A
        Assert.That(lines[2], Does.Contain("2024-03-15"));
        Assert.That(lines[3], Does.Contain("User B")); // User B comes after User A
    }

    [Test]
    public void GenerateMonthlyCsv_WithEmptyList_ShouldCreateHeaderOnly()
    {
        // Arrange
        var sessions = new List<ChargingSession>();

        // Act
        var csvData = _generator.GenerateMonthlyCsv(sessions, 2024, 3);
        var csvText = System.Text.Encoding.UTF8.GetString(csvData);

        // Assert
        Assert.That(csvData, Is.Not.Null);
        Assert.That(csvData.Length, Is.GreaterThan(0));
        Assert.That(csvText, Does.Contain("User,Start,End,Duration (minutes),Energy (kWh)"));
        var lines = csvText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.That(lines.Length, Is.EqualTo(1)); // Only header
    }

    [Test]
    public void GenerateMonthlyCsv_WithSpecialCharactersInUserName_ShouldEscapeProperly()
    {
        // Arrange
        var sessions = new List<ChargingSession>
        {
            new()
            {
                UserId = "user1",
                UserName = "Test, User",
                CarConnected = new DateTime(2024, 3, 15, 10, 0, 0),
                KiloWattHours = 25.5
            }
        };

        // Act
        var csvData = _generator.GenerateMonthlyCsv(sessions, 2024, 3);
        var csvText = System.Text.Encoding.UTF8.GetString(csvData);

        // Assert
        Assert.That(csvText, Does.Contain("\"Test, User\""));
    }

    [Test]
    public void GenerateMonthlyCsv_WithNullOptionalFields_ShouldHandleGracefully()
    {
        // Arrange
        var sessions = new List<ChargingSession>
        {
            new()
            {
                UserId = "user1",
                UserName = "Test User",
                CarConnected = new DateTime(2024, 3, 15, 10, 0, 0),
                CarDisconnected = null,
                KiloWattHours = 25.5,
                ActualDurationSeconds = null
            }
        };

        // Act
        var csvData = _generator.GenerateMonthlyCsv(sessions, 2024, 3);
        var csvText = System.Text.Encoding.UTF8.GetString(csvData);

        // Assert
        Assert.That(csvData, Is.Not.Null);
        Assert.That(csvData.Length, Is.GreaterThan(0));
        Assert.That(csvText, Does.Contain("Test User"));
        Assert.That(csvText, Does.Contain("25.50"));
    }
}
