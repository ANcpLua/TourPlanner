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
        var reports = endpoints.MapGroup("/api/reports").WithTags("Reports");
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

    internal static Results<FileContentHttpResult, NotFound> GetTourReport(
        Guid tourId,
        IFileService fileService)
    {
        var report = fileService.GenerateTourReport(tourId);
        return report is null
            ? TypedResults.NotFound()
            : TypedResults.File(report, "application/pdf", $"TourReport_{tourId}.pdf");
    }

    internal static Results<JsonHttpResult<TourDto>, NotFound> ExportTourToJson(
        Guid tourId,
        IFileService fileService,
        IMapper mapper)
    {
        if (fileService.ExportTourToJson(tourId) is not { } tourDomain)
            return TypedResults.NotFound();

        return TypedResults.Json(mapper.Map<TourDto>(tourDomain), ExportJsonOptions);
    }

    internal static async Task<Results<Ok<string>, BadRequest<string>>> ImportTourFromJsonAsync(
        string json,
        IFileService fileService,
        CancellationToken cancellationToken)
    {
        return await fileService.ImportTourFromJsonAsync(json, cancellationToken)
            ? TypedResults.Ok("Tour imported successfully")
            : TypedResults.BadRequest("Invalid or empty tour data.");
    }
}
