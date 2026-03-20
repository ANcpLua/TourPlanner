using System.Net;
using API.AOP;
using BL.DomainModel;
using BL.Interface;
using Contracts.TourLogs;
using MapsterMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
[ApiController]
[Route("api/tourlog")]
public class TourLogController(ITourLogService tourLogService, IMapper mapper) : ControllerBase
{
    [ApiMethodDecorator]
    [HttpPost]
    [ProducesResponseType(typeof(TourLogDto), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> CreateTourLog(
        [FromBody] TourLogDto tourLogDto,
        CancellationToken cancellationToken = default
    )
    {
        var tourLog = mapper.Map<TourLogDomain>(tourLogDto);
        var createdTourLog = await tourLogService.CreateTourLogAsync(tourLog, cancellationToken);
        var createdTourLogDto = mapper.Map<TourLogDto>(createdTourLog);
        return Ok(createdTourLogDto);
    }

    [ApiMethodDecorator]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TourLogDto), (int)HttpStatusCode.OK)]
    public ActionResult GetTourLogById(Guid id)
    {
        if (tourLogService.GetTourLogById(id) is not { } tourLog) return NotFound();
        var tourLogDto = mapper.Map<TourLogDto>(tourLog);
        return Ok(tourLogDto);
    }

    [ApiMethodDecorator]
    [HttpGet("bytour/{tourId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TourLogDto>), (int)HttpStatusCode.OK)]
    public ActionResult GetTourLogsByTourId(Guid tourId)
    {
        var tourLogs = tourLogService.GetTourLogsByTourId(tourId);
        var tourLogDtos = mapper.Map<IEnumerable<TourLogDto>>(tourLogs);
        return Ok(tourLogDtos);
    }

    [ApiMethodDecorator]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TourLogDto), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> UpdateTourLog(
        Guid id,
        [FromBody] TourLogDto tourLogDto,
        CancellationToken cancellationToken = default
    )
    {
        var tourLog = mapper.Map<TourLogDomain>(tourLogDto);
        var updatedTourLog = await tourLogService.UpdateTourLogAsync(tourLog, cancellationToken);
        var updatedTourLogDto = mapper.Map<TourLogDto>(updatedTourLog);
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
