using System.Diagnostics;
using System.Text;
using BL.DomainModel;
using BL.Interface;
using Contracts.Reports;
using Contracts.Tours;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;

namespace API.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var reports = endpoints.MapGroup(ApiRoute.Reports.Base).WithTags(ApiTag.Reports);
        reports.MapGet("/summary", GetSummaryReport);
        reports.MapGet("/tour/{tourId:guid}", GetTourReport);
        reports.MapGet("/export/{tourId:guid}", ExportTourToXml)
            .Produces<string>(StatusCodes.Status200OK, "application/xml")
            .Produces(StatusCodes.Status404NotFound);
        reports.MapPost("/import", ImportTourFromXmlAsync)
            .Accepts<ImportTourRequest>("application/json")
            .Produces<TourDto>(StatusCodes.Status201Created, "application/json")
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);
        return endpoints;
    }

    internal static FileContentHttpResult GetSummaryReport(
        IPdfReportService pdfReportService,
        ITourService tourService)
    {
        var report = pdfReportService.GenerateSummaryReport(tourService.GetAllTours());
        return TypedResults.File(report, "application/pdf", "SummaryReport.pdf");
    }

    internal static Results<FileContentHttpResult, NotFound> GetTourReport(
        Guid tourId,
        ITourService tourService,
        IPdfReportService pdfReportService)
    {
        if (tourService.GetTourById(tourId) is not { } tour)
            return TypedResults.NotFound();

        var report = pdfReportService.GenerateTourReport(tour);
        return TypedResults.File(report, "application/pdf", $"TourReport_{tourId}.pdf");
    }

    internal static Results<ContentHttpResult, NotFound> ExportTourToXml(
        Guid tourId,
        ITourService tourService)
    {
        if (tourService.GetTourById(tourId) is not { } tour)
            return TypedResults.NotFound();

        var xml = ToXmlDocument(tour).WriteToXml();
        return TypedResults.Text(xml, "application/xml", Encoding.UTF8);
    }

    internal static async Task<Results<Created<TourDto>, ValidationProblem>> ImportTourFromXmlAsync(
        [FromBody] ImportTourRequest request,
        ITourService tourService,
        IMapper mapper,
        CancellationToken cancellationToken)
    {
        var parseResult = TourXmlParser.Parse(request.Xml);
        if (parseResult is TourXmlParseResult.Invalid invalid)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(ImportTourRequest.Xml)] = [invalid.Error]
            });
        }

        var document = ((TourXmlParseResult.Parsed)parseResult).Document;
        var importedTour = await tourService.CreateTourAsync(ToDomain(document), cancellationToken);
        var tour = mapper.Map<TourDto>(importedTour);
        return TypedResults.Created(ApiRoute.Tour.ById(tour.Id), tour);
    }

    private static TourXmlDocument ToXmlDocument(TourDomain tour)
    {
        return new TourXmlDocument
        {
            Name = tour.Name,
            Description = tour.Description,
            From = tour.From,
            To = tour.To,
            ImagePath = tour.ImagePath,
            RouteInformation = tour.RouteInformation,
            Distance = tour.Distance,
            EstimatedTime = tour.EstimatedTime,
            TransportType = tour.TransportType,
            TourLogs = tour.Logs.Select(static log => new TourLogXmlItem
            {
                DateTime = log.DateTime,
                Comment = log.Comment,
                Difficulty = log.Difficulty,
                TotalDistance = log.TotalDistance,
                TotalTime = log.TotalTime,
                Rating = log.Rating
            }).ToArray()
        };
    }

    private static TourDomain ToDomain(TourXmlDocument document)
    {
        return new TourDomain
        {
            Id = Guid.Empty,
            Name = document.Name,
            Description = document.Description,
            From = document.From,
            To = document.To,
            ImagePath = document.ImagePath,
            RouteInformation = document.RouteInformation,
            Distance = document.Distance,
            EstimatedTime = document.EstimatedTime,
            TransportType = document.TransportType,
            Logs = document.TourLogs.Select(static log => new TourLogDomain
            {
                Id = Guid.Empty,
                TourDomainId = Guid.Empty,
                DateTime = log.DateTime,
                Comment = log.Comment,
                Difficulty = RequiredValue(log.Difficulty),
                TotalDistance = RequiredValue(log.TotalDistance),
                TotalTime = RequiredValue(log.TotalTime),
                Rating = RequiredValue(log.Rating)
            }).ToList()
        };
    }

    private static double RequiredValue(double? value) =>
        value ?? throw new UnreachableException("The XML parser returned a missing required number.");
}
