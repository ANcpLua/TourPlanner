using System.Net;
using API.AOP;
using BL.DomainModel;
using BL.Interface;
using Contracts.Tours;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/tour")]
public class TourController(ITourService tourService, IMapper mapper) : ControllerBase
{
    [ApiMethodDecorator]
    [HttpPost]
    [ProducesResponseType(typeof(TourDto), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<TourDto>> CreateTour(
        [FromBody] TourDto tourDto,
        CancellationToken cancellationToken = default)
    {
        var tourDomain = mapper.Map<TourDomain>(tourDto);
        var createdTour = await tourService.CreateTourAsync(tourDomain, cancellationToken);
        return Ok(mapper.Map<TourDto>(createdTour));
    }

    [ApiMethodDecorator]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TourDto>), (int)HttpStatusCode.OK)]
    public ActionResult<IEnumerable<TourDto>> GetAllTours()
    {
        var tours = tourService.GetAllTours();
        return Ok(mapper.Map<IEnumerable<TourDto>>(tours));
    }

    [ApiMethodDecorator]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TourDto), (int)HttpStatusCode.OK)]
    public ActionResult<TourDto> GetTourById(Guid id)
    {
        var tour = tourService.GetTourById(id);
        if (tour is null) return NotFound();
        return Ok(mapper.Map<TourDto>(tour));
    }

    [ApiMethodDecorator]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TourDto), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<TourDto>> UpdateTour(
        Guid id,
        [FromBody] TourDto tourDto,
        CancellationToken cancellationToken = default)
    {
        if (id != tourDto.Id) return BadRequest("ID mismatch");
        var tourDomain = mapper.Map<TourDomain>(tourDto);
        var updatedTour = await tourService.UpdateTourAsync(tourDomain, cancellationToken);
        return Ok(mapper.Map<TourDto>(updatedTour));
    }

    [ApiMethodDecorator]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    public async Task<ActionResult> DeleteTour(Guid id, CancellationToken cancellationToken = default)
    {
        await tourService.DeleteTourAsync(id, cancellationToken);
        return NoContent();
    }

    [ApiMethodDecorator]
    [HttpGet("search/{searchText}")]
    [ProducesResponseType(typeof(IEnumerable<TourDto>), (int)HttpStatusCode.OK)]
    public ActionResult SearchTours(string searchText)
    {
        var tours = tourService.SearchTours(searchText);
        var tourDtos = mapper.Map<IEnumerable<TourDto>>(tours);
        return Ok(tourDtos);
    }
}
