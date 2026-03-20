using BL.DomainModel;

namespace BL.Interface;

public interface IFileService
{
    byte[]? GenerateTourReport(Guid tourId);
    TourDomain? ExportTourToJson(Guid tourId);
    byte[] GenerateSummaryReport(IEnumerable<TourDomain> tours);
    Task<bool> ImportTourFromJsonAsync(string json, CancellationToken cancellationToken = default);
}