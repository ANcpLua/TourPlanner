using System.Net;
using API.AOP;
using BL.DomainModel;
using BL.Interface;
using MapsterMapper;
using Microsoft.AspNetCore.Mvc;
using UI.Model;

namespace API.Controllers;

[ApiController]
[Route("api/tour")]
public class TourController(ITourService tourService, IMapper mapper) : ControllerBase
{
    [ApiMethodDecorator]
    [HttpPost]
    [ProducesResponseType(typeof(Tour), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<Tour>> CreateTour(
        [FromBody] Tour tourDto,
        CancellationToken cancellationToken = default)
    {
        var tourDomain = mapper.Map<TourDomain>(tourDto);
        var createdTour = await tourService.CreateTourAsync(tourDomain, cancellationToken);
        return Ok(mapper.Map<Tour>(createdTour));
    }

    [ApiMethodDecorator]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Tour>), (int)HttpStatusCode.OK)]
    public ActionResult<IEnumerable<Tour>> GetAllTours()
    {
        var tours = tourService.GetAllTours();
        return Ok(mapper.Map<IEnumerable<Tour>>(tours));
    }

    [ApiMethodDecorator]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Tour), (int)HttpStatusCode.OK)]
    public ActionResult<Tour> GetTourById(Guid id)
    {
        var tour = tourService.GetTourById(id);
        if (tour is null) return NotFound();
        return Ok(mapper.Map<Tour>(tour));
    }

    [ApiMethodDecorator]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Tour), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<Tour>> UpdateTour(
        Guid id,
        [FromBody] Tour tourDto,
        CancellationToken cancellationToken = default)
    {
        if (id != tourDto.Id) return BadRequest("ID mismatch");
        var tourDomain = mapper.Map<TourDomain>(tourDto);
        var updatedTour = await tourService.UpdateTourAsync(tourDomain, cancellationToken);
        return Ok(mapper.Map<Tour>(updatedTour));
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
    [ProducesResponseType(typeof(IEnumerable<Tour>), (int)HttpStatusCode.OK)]
    public ActionResult SearchTours(string searchText)
    {
        var tours = tourService.SearchTours(searchText);
        var tourDtos = mapper.Map<IEnumerable<Tour>>(tours);
        return Ok(tourDtos);
    }
}