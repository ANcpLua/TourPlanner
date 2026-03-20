using System.Text.Json;
using BL.Interface;
using Contracts.Tours;
using MapsterMapper;
using Microsoft.AspNetCore.Http.HttpResults;

namespace API.Endpoints;

public static class ReportEndpoints
{
    private static readonly JsonSerializerOptions ExportJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var reports = endpoints.MapGroup("/api/reports").WithTags("Reports").RequireAuthorization();
        reports.MapGet("/summary", GetSummaryReport);
        reports.MapGet("/tour/{tourId:guid}", GetTourReport);
        reports.MapGet("/export/{tourId:guid}", ExportTourToJson);
        reports.MapPost("/import", ImportTourFromJsonAsync);
        return endpoints;
    }

    internal static FileContentHttpResult GetSummaryReport(
        IFileService fileService,
        ITourService tourService)
    {
        var report = fileService.GenerateSummaryReport(tourService.GetAllTours());
        return TypedResults.File(report, "application/pdf", "SummaryReport.pdf");
    }

    internal static FileContentHttpResult GetTourReport(
        Guid tourId,
        IFileService fileService)
    {
        var report = fileService.GenerateTourReport(tourId);
        return TypedResults.File(report, "application/pdf", $"TourReport_{tourId}.pdf");
    }

    internal static JsonHttpResult<TourDto> ExportTourToJson(
        Guid tourId,
        IFileService fileService,
        IMapper mapper)
    {
        var tourDomain = fileService.ExportTourToJson(tourId);
        var tourDto = mapper.Map<TourDto>(tourDomain);
        return TypedResults.Json(tourDto, ExportJsonOptions);
    }

    internal static async Task<Ok<string>> ImportTourFromJsonAsync(
        string json,
        IFileService fileService,
        CancellationToken cancellationToken)
    {
        await fileService.ImportTourFromJsonAsync(json, cancellationToken);
        return TypedResults.Ok("Tour imported successfully");
    }
}
