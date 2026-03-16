using System.Text.Json;
using BL.DomainModel;
using BL.Interface;

namespace BL.Service;

public class FileService(ITourService tourService, IPdfReportService pdfReportService) : IFileService
{
    public byte[] GenerateTourReport(Guid tourId)
    {
        var tour = tourService.GetTourById(tourId)
                   ?? throw new InvalidOperationException($"Tour with ID '{tourId}' not found.");
        return pdfReportService.GenerateTourReport(tour);
    }

    public byte[] GenerateSummaryReport(IEnumerable<TourDomain> tours)
    {
        return pdfReportService.GenerateSummaryReport(tours);
    }

    public TourDomain ExportTourToJson(Guid tourId)
    {
        return tourService.GetTourById(tourId)
               ?? throw new InvalidOperationException($"Tour with ID '{tourId}' not found.");
    }

    public async Task ImportTourFromJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        var tour = JsonSerializer.Deserialize<TourDomain>(json);
        if (tour is not null) await tourService.CreateTourAsync(tour, cancellationToken);
    }
}