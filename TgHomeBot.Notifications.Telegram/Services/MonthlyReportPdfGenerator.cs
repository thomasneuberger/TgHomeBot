using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TgHomeBot.Charging.Contract.Models;
using TgHomeBot.Charging.Contract.Services;

namespace TgHomeBot.Notifications.Telegram.Services;

internal class MonthlyReportPdfGenerator : IMonthlyReportPdfGenerator
{
    public byte[] GenerateMonthlyPdf(IReadOnlyList<ChargingSession> sessions, int year, int month)
    {
        var monthName = new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("de-DE"));
        
        // Group by user and sum the energy
        var userSummaries = sessions
            .GroupBy(s => s.UserName)
            .Select(g => new
            {
                UserName = g.Key,
                TotalKwh = g.Sum(s => s.KiloWattHours),
                SessionCount = g.Count()
            })
            .OrderBy(x => x.UserName)
            .ToList();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Text($"Monatlicher Ladebericht - {monthName}")
                    .SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(20);

                        column.Item().Text("Zusammenfassung").SemiBold().FontSize(16);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("Benutzer").SemiBold();
                                header.Cell().Element(CellStyle).Text("Ladevorgänge").SemiBold();
                                header.Cell().Element(CellStyle).Text("Gesamt (kWh)").SemiBold();
                            });

                            foreach (var summary in userSummaries)
                            {
                                table.Cell().Element(CellStyle).Text(summary.UserName);
                                table.Cell().Element(CellStyle).Text(summary.SessionCount.ToString());
                                table.Cell().Element(CellStyle).Text($"{summary.TotalKwh:F2}");
                            }
                        });

                        var totalKwh = userSummaries.Sum(s => s.TotalKwh);
                        var totalSessions = userSummaries.Sum(s => s.SessionCount);
                        
                        column.Item().Text($"Gesamt: {totalSessions} Ladevorgänge, {totalKwh:F2} kWh").SemiBold();
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Seite ");
                        x.CurrentPageNumber();
                        x.Span(" von ");
                        x.TotalPages();
                    });
            });
        });

        return document.GeneratePdf();
    }

    public string GetFileName(int year, int month)
    {
        // Format: YYYYMM-charging.pdf (17 characters, well under 20 char limit)
        return $"{year:D4}{month:D2}-charging.pdf";
    }

    private static IContainer CellStyle(IContainer container)
    {
        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
    }
}
