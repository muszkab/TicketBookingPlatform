using Application.Common.Models;
using Application.Locations;
using Application.Locations.Commands.CreateLocation;
using Application.Locations.Commands.DeleteLocation;
using Application.Locations.Commands.UpdateLocation;
using Application.Locations.Queries.GetEventsByLocation;
using Application.Locations.Queries.GetLocationById;
using Application.Locations.Queries.GetLocations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Contracts.Locations;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocationsController : ControllerBase
{
    private readonly GetLocationsQueryHandler _getLocationsQueryHandler;
    private readonly GetLocationByIdQueryHandler _getLocationByIdQueryHandler;
    private readonly GetEventsByLocationQueryHandler _getEventsByLocationQueryHandler;
    private readonly CreateLocationCommandHandler _createLocationCommandHandler;
    private readonly UpdateLocationCommandHandler _updateLocationCommandHandler;
    private readonly DeleteLocationCommandHandler _deleteLocationCommandHandler;

    public LocationsController(
        GetLocationsQueryHandler getLocationsQueryHandler,
        GetLocationByIdQueryHandler getLocationByIdQueryHandler,
        GetEventsByLocationQueryHandler getEventsByLocationQueryHandler,
        CreateLocationCommandHandler createLocationCommandHandler,
        UpdateLocationCommandHandler updateLocationCommandHandler,
        DeleteLocationCommandHandler deleteLocationCommandHandler)
    {
        _getLocationsQueryHandler = getLocationsQueryHandler;
        _getLocationByIdQueryHandler = getLocationByIdQueryHandler;
        _getEventsByLocationQueryHandler = getEventsByLocationQueryHandler;
        _createLocationCommandHandler = createLocationCommandHandler;
        _updateLocationCommandHandler = updateLocationCommandHandler;
        _deleteLocationCommandHandler = deleteLocationCommandHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<LocationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LocationDto>>> GetAll(
        [FromQuery] string? name,
        [FromQuery] string? city,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetLocationsQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _getLocationsQueryHandler.HandleAsync(
            new GetLocationsQuery(name, city, page, pageSize),
            cancellationToken);
        return Ok(result);
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

    [HttpPost]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LocationDto>> Create([FromBody] CreateLocationRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateLocationCommand(
            request.Name,
            request.Street,
            request.City,
            request.PostalCode,
            request.Country,
            request.Capacity);

        LocationDto created = await _createLocationCommandHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateLocationCommand(
            id,
            request.Name,
            request.Street,
            request.City,
            request.PostalCode,
            request.Country,
            request.Capacity);

        await _updateLocationCommandHandler.HandleAsync(command, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _deleteLocationCommandHandler.HandleAsync(new DeleteLocationCommand(id), cancellationToken);

        return NoContent();
    }
}
