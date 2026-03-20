using System.Globalization;
using BL.DomainModel;
using BL.Interface;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Drawing.Exceptions;
using QuestPDF.Infrastructure;

namespace BL.Service;

public class PdfReportService(Func<string, byte[]>? imageLoader = null) : IPdfReportService
{
    static PdfReportService() => Settings.License = LicenseType.Community;

    private readonly Func<string, byte[]> _imageLoader = imageLoader ?? File.ReadAllBytes;

    public byte[] GenerateTourReport(TourDomain tour)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Text("Tour Report").SemiBold().FontSize(20).AlignCenter();

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                {
                    AddTourDetails(column, tour);
                    AddTourImage(column, tour.ImagePath);
                    if (tour.Logs.Count > 0) AddTourLogs(column, tour.Logs);
                });

                AddPageFooter(page);
            });
        }).GeneratePdf();
    }

    public byte[] GenerateSummaryReport(IEnumerable<TourDomain> tours)
    {
        var tourList = tours.ToList();

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Text("Summary Report").SemiBold().FontSize(20).AlignCenter();

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                {
                    AddSummaryStatistics(column, tourList);
                    column.Item().PaddingVertical(10).LineHorizontal(1);
                    AddSummaryTable(column, tourList);
                });

                AddPageFooter(page);
            });
        }).GeneratePdf();
    }

    private static void AddSummaryStatistics(ColumnDescriptor column, List<TourDomain> tours)
    {
        var totalLogs = tours.Sum(t => t.Logs.Count);
        var avgDistance = tours.Where(t => t.Distance.HasValue).Select(t => t.Distance!.Value).DefaultIfEmpty(0).Average();
        var avgTime = tours.Where(t => t.EstimatedTime.HasValue).Select(t => t.EstimatedTime!.Value).DefaultIfEmpty(0).Average();

        column.Item().PaddingBottom(10).Text("Overview").SemiBold().FontSize(14);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(140);
                columns.RelativeColumn();
            });

            AddTableRow(table, "Total Tours:", tours.Count.ToString(CultureInfo.InvariantCulture));
            AddTableRow(table, "Total Logs:", totalLogs.ToString(CultureInfo.InvariantCulture));
            AddTableRow(table, "Avg. Distance:", FormatDistance(avgDistance));
            AddTableRow(table, "Avg. Est. Time:", FormatTime(avgTime));
        });
    }

    private static void AddSummaryTable(ColumnDescriptor column, List<TourDomain> tours)
    {
        column.Item().PaddingBottom(10).Text("All Tours").SemiBold().FontSize(14);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2);
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.RelativeColumn();
                columns.ConstantColumn(50);
            });

            table.Header(header =>
            {
                header.Cell().Text("Name").SemiBold();
                header.Cell().Text("Route").SemiBold();
                header.Cell().Text("Transport").SemiBold();
                header.Cell().Text("Distance").SemiBold();
                header.Cell().Text("Logs").SemiBold();
            });

            foreach (var tour in tours)
            {
                table.Cell().Text(tour.Name);
                table.Cell().Text($"{tour.From} → {tour.To}");
                table.Cell().Text(tour.TransportType);
                table.Cell().Text(FormatDistance(tour.Distance));
                table.Cell().Text(tour.Logs.Count.ToString(CultureInfo.InvariantCulture));
            }
        });
    }

    private static void AddTourDetails(ColumnDescriptor column, TourDomain tour)
    {
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(100);
                columns.RelativeColumn();
            });

            AddTableRow(table, "Tour Name:", tour.Name);
            AddTableRow(table, "Description:", tour.Description);
            AddTableRow(table, "From:", tour.From);
            AddTableRow(table, "To:", tour.To);
            AddTableRow(table, "Distance:", FormatDistance(tour.Distance));
            AddTableRow(table, "Est. Time:", FormatTime(tour.EstimatedTime));
            AddTableRow(table, "Transport:", tour.TransportType);
        });
    }

    private static void AddTableRow(TableDescriptor table, string label, string value)
    {
        table.Cell().Text(label);
        table.Cell().Text(value);
    }

    private static string FormatDistance(double? distance)
    {
        return distance?.ToString("N2", CultureInfo.InvariantCulture) is { } value
            ? $"{value} meters"
            : "N/A";
    }

    private static string FormatTime(double? time)
    {
        return time?.ToString("N2", CultureInfo.InvariantCulture) is { } value
            ? $"{value} minutes"
            : "N/A";
    }

    private void AddTourImage(ColumnDescriptor column, string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) return;

        try
        {
            var imageBytes = _imageLoader(imagePath);
            column.Item().Image(imageBytes).FitWidth();
        }
        catch (Exception ex) when (ex is IOException or DocumentComposeException)
        {
            column.Item().Background(Colors.Grey.Lighten3)
                .Padding(10)
                .Text($"Error loading image: {ex.Message}")
                .FontSize(10);
        }
    }

    private static void AddTourLogs(ColumnDescriptor column, IEnumerable<TourLogDomain> tourLogs)
    {
        column.Item().PaddingTop(10).Text("Tour Logs:").SemiBold();

        foreach (var log in tourLogs)
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(100);
                    columns.RelativeColumn();
                });

                AddTableRow(table, "Date:", log.DateTime.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture));
                AddTableRow(table, "Comment:", log.Comment ?? "N/A");
                AddTableRow(table, "Difficulty:", log.Difficulty.ToString(CultureInfo.InvariantCulture));
                AddTableRow(table, "Rating:", log.Rating.ToString(CultureInfo.InvariantCulture));
                AddTableRow(table, "Distance:",
                    $"{log.TotalDistance.ToString("N2", CultureInfo.InvariantCulture)} meters");
                AddTableRow(table, "Time:", $"{log.TotalTime.ToString("N2", CultureInfo.InvariantCulture)} minutes");
            });
    }

    private static void AddPageFooter(PageDescriptor page)
    {
        page.Footer().AlignCenter().Text(static x =>
        {
            x.Span("Page ");
            x.CurrentPageNumber();
            x.Span(" of ");
            x.TotalPages();
        });
    }
}
