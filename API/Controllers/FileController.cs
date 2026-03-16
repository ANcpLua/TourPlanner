using System.Net;
using System.Text.Json;
using API.AOP;
using BL.Interface;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using UI.Model;

namespace API.Controllers;

[ApiController]
[Route("api/reports")]
public class FileController(IFileService fileService, ITourService tourService, IMapper mapper) : ControllerBase
{
    [ApiMethodDecorator]
    [HttpGet("summary")]
    [ProducesResponseType(typeof(FileResult), (int)HttpStatusCode.OK)]
    public FileResult GetSummaryReport()
    {
        var tours = tourService.GetAllTours();
        var report = fileService.GenerateSummaryReport(tours);
        return File(report, "application/pdf", "SummaryReport.pdf");
    }

    [ApiMethodDecorator]
    [HttpGet("tour/{tourId:guid}")]
    [ProducesResponseType(typeof(FileResult), (int)HttpStatusCode.OK)]
    public FileResult GetTourReport(Guid tourId)
    {
        var report = fileService.GenerateTourReport(tourId);
        return File(report, "application/pdf", $"TourReport_{tourId}.pdf");
    }

    [ApiMethodDecorator]
    [HttpGet("export/{tourId:guid}")]
    [ProducesResponseType(typeof(Tour), (int)HttpStatusCode.OK)]
    public ActionResult ExportTourToJson(Guid tourId)
    {
        var tourDomain = fileService.ExportTourToJson(tourId);
        var tourDto = mapper.Map<Tour>(tourDomain);
        return new JsonResult(tourDto,
            new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase })
        {
            ContentType = "application/json", StatusCode = (int)HttpStatusCode.OK
        };
    }

    [ApiMethodDecorator]
    [HttpPost("import")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    public async Task<IActionResult> ImportTourFromJsonAsync([FromBody] string json)
    {
        await fileService.ImportTourFromJsonAsync(json);
        return Ok("Tour imported successfully");
    }
}
