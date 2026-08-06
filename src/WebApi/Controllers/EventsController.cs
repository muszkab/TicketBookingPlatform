using Application.Common.Models;
using Application.Events;
using Application.Events.Queries.GetEventById;
using Application.Events.Queries.GetEvents;
using Domain.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Controllers;

public class EventsController : ApiControllerBase
{
    private readonly GetEventByIdQueryHandler _getEventByIdQueryHandler;
    private readonly GetEventsQueryHandler _getEventsQueryHandler;

    public EventsController(
        GetEventByIdQueryHandler getEventByIdQueryHandler,
        GetEventsQueryHandler getEventsQueryHandler)
    {
        _getEventByIdQueryHandler = getEventByIdQueryHandler;
        _getEventsQueryHandler = getEventsQueryHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetAll(
        [FromQuery] EventCategory? category,
        [FromQuery] EventStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetEventsQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        PagedResult<EventDto> result = await _getEventsQueryHandler.HandleAsync(new GetEventsQuery(category, status, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        EventDto? eventData = await _getEventByIdQueryHandler.HandleAsync(new GetEventByIdQuery(id), cancellationToken);
        return eventData is null ? NotFound() : Ok(eventData);
    }
}
