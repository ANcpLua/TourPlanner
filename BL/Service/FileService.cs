using System.Text.Json;
using BL.DomainModel;
using BL.Interface;

namespace BL.Service;

public class FileService(ITourService tourService, IPdfReportService pdfReportService) : IFileService
{
    public byte[] GenerateTourReport(Guid tourId)
    {
        var tour = tourService.GetTourById(tourId);
        return pdfReportService.GenerateTourReport(tour);
    }

    public byte[] GenerateSummaryReport(IEnumerable<TourDomain> tours) =>
        pdfReportService.GenerateSummaryReport(tours);

    public TourDomain ExportTourToJson(Guid tourId) => tourService.GetTourById(tourId);

    public async Task ImportTourFromJsonAsync(string json)
    {
        var tour = JsonSerializer.Deserialize<TourDomain>(json);
        if (tour is not null) await tourService.CreateTourAsync(tour);
    }
}
