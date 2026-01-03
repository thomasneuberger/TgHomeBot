using NUnit.Framework;
using QuestPDF.Infrastructure;
using TgHomeBot.Charging.Contract.Models;
using TgHomeBot.Notifications.Telegram.Services;

namespace TgHomeBot.Notifications.Telegram.Tests.Services;

[TestFixture]
public class MonthlyReportPdfGeneratorTests
{
    private MonthlyReportPdfGenerator _generator = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Configure QuestPDF license for tests
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [SetUp]
    public void SetUp()
    {
        _generator = new MonthlyReportPdfGenerator();
    }

    [Test]
    public void GetFileName_ShouldReturnCorrectFormat()
    {
        // Act
        var fileName = _generator.GetFileName(2024, 3);

        // Assert
        Assert.That(fileName, Is.EqualTo("202403-charging.pdf"));
    }

    [Test]
    public void GetFileName_ShouldBeUnder20Characters()
    {
        // Act
        var fileName = _generator.GetFileName(2024, 12);
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        // Assert
        Assert.That(nameWithoutExtension.Length, Is.LessThanOrEqualTo(20));
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
        Assert.That(sorted[0], Is.EqualTo("202306-charging.pdf"));
        Assert.That(sorted[1], Is.EqualTo("202401-charging.pdf"));
        Assert.That(sorted[2], Is.EqualTo("202403-charging.pdf"));
        Assert.That(sorted[3], Is.EqualTo("202412-charging.pdf"));
    }

    [Test]
    public void GenerateMonthlyPdf_ShouldCreateValidPdf()
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
                KiloWattHours = 25.5
            },
            new()
            {
                UserId = "user2",
                UserName = "Test User 2",
                CarConnected = new DateTime(2024, 3, 20, 14, 0, 0),
                CarDisconnected = new DateTime(2024, 3, 20, 16, 0, 0),
                KiloWattHours = 30.2
            }
        };

        // Act
        var pdfData = _generator.GenerateMonthlyPdf(sessions, 2024, 3);

        // Assert
        Assert.That(pdfData, Is.Not.Null);
        Assert.That(pdfData.Length, Is.GreaterThan(0));
        // Check PDF signature (should start with "%PDF")
        Assert.That(pdfData[0], Is.EqualTo((byte)'%'));
        Assert.That(pdfData[1], Is.EqualTo((byte)'P'));
        Assert.That(pdfData[2], Is.EqualTo((byte)'D'));
        Assert.That(pdfData[3], Is.EqualTo((byte)'F'));
    }

    [Test]
    public void GenerateMonthlyPdf_WithSingleUser_ShouldCreateValidPdf()
    {
        // Arrange
        var sessions = new List<ChargingSession>
        {
            new()
            {
                UserId = "user1",
                UserName = "Test User",
                CarConnected = new DateTime(2024, 3, 15, 10, 0, 0),
                CarDisconnected = new DateTime(2024, 3, 15, 12, 0, 0),
                KiloWattHours = 25.5
            }
        };

        // Act
        var pdfData = _generator.GenerateMonthlyPdf(sessions, 2024, 3);

        // Assert
        Assert.That(pdfData, Is.Not.Null);
        Assert.That(pdfData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void GenerateMonthlyPdf_WithMultipleSessions_ShouldCreateValidPdf()
    {
        // Arrange
        var sessions = new List<ChargingSession>();
        for (int i = 0; i < 10; i++)
        {
            sessions.Add(new ChargingSession
            {
                UserId = $"user{i % 3}",
                UserName = $"Test User {i % 3}",
                CarConnected = new DateTime(2024, 3, i + 1, 10, 0, 0),
                CarDisconnected = new DateTime(2024, 3, i + 1, 12, 0, 0),
                KiloWattHours = 20.0 + i
            });
        }

        // Act
        var pdfData = _generator.GenerateMonthlyPdf(sessions, 2024, 3);

        // Assert
        Assert.That(pdfData, Is.Not.Null);
        Assert.That(pdfData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void GenerateMonthlyPdf_WithEmptyList_ShouldCreateValidPdf()
    {
        // Arrange
        var sessions = new List<ChargingSession>();

        // Act
        var pdfData = _generator.GenerateMonthlyPdf(sessions, 2024, 3);

        // Assert
        Assert.That(pdfData, Is.Not.Null);
        Assert.That(pdfData.Length, Is.GreaterThan(0));
    }
}
