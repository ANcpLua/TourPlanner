using System.Text.Json;
using BL.DomainModel;
using BL.Interface;

namespace BL.Service;

public class FileService(ITourService tourService, IPdfReportService pdfReportService) : IFileService
{
    public byte[]? GenerateTourReport(Guid tourId)
    {
        var tour = tourService.GetTourById(tourId);
        return tour is null ? null : pdfReportService.GenerateTourReport(tour);
    }

    public byte[] GenerateSummaryReport(IEnumerable<TourDomain> tours) =>
        pdfReportService.GenerateSummaryReport(tours);

    public TourDomain? ExportTourToJson(Guid tourId) =>
        tourService.GetTourById(tourId);

    public async Task<bool> ImportTourFromJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        TourDomain? tour;
        try
        {
            tour = JsonSerializer.Deserialize<TourDomain>(json);
        }
        catch (JsonException)
        {
            return false;
        }

        if (tour is null) return false;
        await tourService.CreateTourAsync(tour, cancellationToken);
        return true;
    }
}
