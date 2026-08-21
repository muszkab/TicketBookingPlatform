using Application.Common.Paging;
using Application.Locations;
using Application.Locations.Commands.CreateLocation;
using Application.Locations.Commands.DeleteLocation;
using Application.Locations.Commands.UpdateLocation;
using Application.Locations.Queries.GetEventsByLocation;
using Application.Locations.Queries.GetLocationById;
using Application.Locations.Queries.GetLocations;
using Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Contracts.Locations;

namespace WebApi.Controllers;

[Authorize(Roles = nameof(UserRole.Admin))]
public class LocationsController(
    GetLocationsQueryHandler getLocationsQueryHandler,
    GetLocationByIdQueryHandler getLocationByIdQueryHandler,
    GetEventsByLocationQueryHandler getEventsByLocationQueryHandler,
    CreateLocationCommandHandler createLocationCommandHandler,
    UpdateLocationCommandHandler updateLocationCommandHandler,
    DeleteLocationCommandHandler deleteLocationCommandHandler)
    : ApiControllerBase
{
    [HttpGet(Name = nameof(GetLocations))]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<LocationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<LocationDto>>> GetLocations(
        [FromQuery] string? name,
        [FromQuery] string? city,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetLocationsQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        PagedResult<LocationDto> result = await getLocationsQueryHandler.HandleAsync(new GetLocationsQuery(name, city, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = nameof(GetLocationById))]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationDto>> GetLocationById(Guid id, CancellationToken cancellationToken)
    {
        LocationDto? location = await getLocationByIdQueryHandler.HandleAsync(new GetLocationByIdQuery(id), cancellationToken);
        return location is null ? NotFound() : Ok(location);
    }

    [HttpGet("{id:guid}/events", Name = nameof(GetEventsByLocation))]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<LocationEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<LocationEventDto>>> GetEventsByLocation(Guid id, CancellationToken cancellationToken)
    {
        IReadOnlyList<LocationEventDto>? events = await getEventsByLocationQueryHandler.HandleAsync(new GetEventsByLocationQuery(id), cancellationToken);
        return events is null ? NotFound() : Ok(events);
    }

    [HttpPost(Name = nameof(CreateLocation))]
    [ProducesResponseType(typeof(LocationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LocationDto>> CreateLocation([FromBody] CreateLocationRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateLocationCommand(
            request.Name,
            request.Street,
            request.City,
            request.PostalCode,
            request.Country,
            request.Capacity);

        LocationDto created = await createLocationCommandHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetLocationById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}", Name = nameof(UpdateLocation))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateLocationCommand(
            id,
            request.Name,
            request.Street,
            request.City,
            request.PostalCode,
            request.Country,
            request.Capacity);

        await updateLocationCommandHandler.HandleAsync(command, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}", Name = nameof(DeleteLocation))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteLocation(Guid id, CancellationToken cancellationToken)
    {
        await deleteLocationCommandHandler.HandleAsync(new DeleteLocationCommand(id), cancellationToken);

        return NoContent();
    }
}
