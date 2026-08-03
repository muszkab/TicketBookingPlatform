using Application.Common.Models;
using Application.Locations.Queries.GetEventsByLocation;
using Application.Locations.Queries.GetLocationById;
using Application.Locations.Queries.GetLocations;
using Application.Locations.Queries.SearchLocations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly GetLocationsQueryHandler _getLocationsQueryHandler;
    private readonly GetLocationByIdQueryHandler _getLocationByIdQueryHandler;
    private readonly GetEventsByLocationQueryHandler _getEventsByLocationQueryHandler;
    private readonly SearchLocationsQueryHandler _searchLocationsQueryHandler;

    public LocationsController(
        GetLocationsQueryHandler getLocationsQueryHandler,
        GetLocationByIdQueryHandler getLocationByIdQueryHandler,
        GetEventsByLocationQueryHandler getEventsByLocationQueryHandler,
        SearchLocationsQueryHandler searchLocationsQueryHandler)
    {
        _getLocationsQueryHandler = getLocationsQueryHandler;
        _getLocationByIdQueryHandler = getLocationByIdQueryHandler;
        _getEventsByLocationQueryHandler = getEventsByLocationQueryHandler;
        _searchLocationsQueryHandler = searchLocationsQueryHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LocationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> GetAll(CancellationToken cancellationToken)
    {
        var locations = await _getLocationsQueryHandler.HandleAsync(new GetLocationsQuery(), cancellationToken);
        return Ok(locations);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var location = await _getLocationByIdQueryHandler.HandleAsync(new GetLocationByIdQuery(id), cancellationToken);
        return location is null ? NotFound() : Ok(location);
    }

    [HttpGet("{id:guid}/events")]
    [ProducesResponseType(typeof(IReadOnlyList<LocationEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<LocationEventDto>>> GetEvents(Guid id, CancellationToken cancellationToken)
    {
        var events = await _getEventsByLocationQueryHandler.HandleAsync(new GetEventsByLocationQuery(id), cancellationToken);
        return events is null ? NotFound() : Ok(events);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<LocationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LocationDto>>> Search(
        [FromQuery] string? name,
        [FromQuery] string? city,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _searchLocationsQueryHandler.HandleAsync(new SearchLocationsQuery(name, city, page, pageSize), cancellationToken);
        return Ok(result);
    }
}
