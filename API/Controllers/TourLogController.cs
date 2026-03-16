using System.Net;
using API.AOP;
using BL.DomainModel;
using BL.Interface;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using UI.Model;

namespace API.Controllers;

[ApiController]
[Route("api/tourlog")]
public class TourLogController(ITourLogService tourLogService, IMapper mapper) : ControllerBase
{
    [ApiMethodDecorator]
    [HttpPost]
    [ProducesResponseType(typeof(TourLog), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> CreateTourLog(
        [FromBody] TourLog tourLogDto,
        CancellationToken cancellationToken = default
    )
    {
        var tourLog = mapper.Map<TourLogDomain>(tourLogDto);
        var createdTourLog = await tourLogService.CreateTourLogAsync(tourLog, cancellationToken);
        var createdTourLogDto = mapper.Map<TourLog>(createdTourLog);
        return Ok(createdTourLogDto);
    }

    [ApiMethodDecorator]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TourLog), (int)HttpStatusCode.OK)]
    public ActionResult GetTourLogById(Guid id)
    {
        var tourLog = tourLogService.GetTourLogById(id);
        if (tourLog is null) return NotFound();
        var tourLogDto = mapper.Map<TourLog>(tourLog);
        return Ok(tourLogDto);
    }

    [ApiMethodDecorator]
    [HttpGet("bytour/{tourId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TourLog>), (int)HttpStatusCode.OK)]
    public ActionResult GetTourLogsByTourId(Guid tourId)
    {
        var tourLogs = tourLogService.GetTourLogsByTourId(tourId);
        var tourLogDtos = mapper.Map<IEnumerable<TourLog>>(tourLogs);
        return Ok(tourLogDtos);
    }

    [ApiMethodDecorator]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TourLog), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateTourLog(
        Guid id,
        [FromBody] TourLog tourLogDto,
        CancellationToken cancellationToken = default
    )
    {
        var tourLog = mapper.Map<TourLogDomain>(tourLogDto);
        var updatedTourLog = await tourLogService.UpdateTourLogAsync(tourLog, cancellationToken);
        var updatedTourLogDto = mapper.Map<TourLog>(updatedTourLog);
        return Ok(updatedTourLogDto);
    }

    [ApiMethodDecorator]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public async Task<IActionResult> DeleteTourLog(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        await tourLogService.DeleteTourLogAsync(id, cancellationToken);
        return NoContent();
    }
}