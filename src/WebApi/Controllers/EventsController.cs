using Application.Events;
using Application.Events.Queries.GetEventById;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Controllers;

public class EventsController : ApiControllerBase
{
    private readonly GetEventByIdQueryHandler _getEventByIdQueryHandler;

    public EventsController(GetEventByIdQueryHandler getEventByIdQueryHandler)
    {
        _getEventByIdQueryHandler = getEventByIdQueryHandler;
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
