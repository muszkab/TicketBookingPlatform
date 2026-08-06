using Application.Common.Paging;
using Application.Events;
using Application.Events.Commands.AddTicketCategory;
using Application.Events.Commands.CancelEvent;
using Application.Events.Commands.CreateEvent;
using Application.Events.Commands.PublishEvent;
using Application.Events.Queries.GetEventById;
using Application.Events.Queries.GetEvents;
using Domain.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Contracts.Events;

namespace WebApi.Controllers;

public class EventsController : ApiControllerBase
{
    private readonly GetEventByIdQueryHandler _getEventByIdQueryHandler;
    private readonly GetEventsQueryHandler _getEventsQueryHandler;
    private readonly CreateEventCommandHandler _createEventCommandHandler;
    private readonly AddTicketCategoryCommandHandler _addTicketCategoryCommandHandler;
    private readonly PublishEventCommandHandler _publishEventCommandHandler;
    private readonly CancelEventCommandHandler _cancelEventCommandHandler;

    public EventsController(
        GetEventByIdQueryHandler getEventByIdQueryHandler,
        GetEventsQueryHandler getEventsQueryHandler,
        CreateEventCommandHandler createEventCommandHandler,
        AddTicketCategoryCommandHandler addTicketCategoryCommandHandler,
        PublishEventCommandHandler publishEventCommandHandler,
        CancelEventCommandHandler cancelEventCommandHandler)
    {
        _getEventByIdQueryHandler = getEventByIdQueryHandler;
        _getEventsQueryHandler = getEventsQueryHandler;
        _createEventCommandHandler = createEventCommandHandler;
        _addTicketCategoryCommandHandler = addTicketCategoryCommandHandler;
        _publishEventCommandHandler = publishEventCommandHandler;
        _cancelEventCommandHandler = cancelEventCommandHandler;
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

    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> Create([FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateEventCommand(
            request.Title,
            request.Description,
            request.Category,
            request.StartsAt,
            request.EndsAt,
            request.LocationId);

        EventDto created = await _createEventCommandHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/ticket-categories")]
    [ProducesResponseType(typeof(TicketCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketCategoryDto>> AddTicketCategory(Guid id, [FromBody] AddTicketCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new AddTicketCategoryCommand(
            id,
            request.Name,
            request.Price,
            request.Currency,
            request.Quantity);

        TicketCategoryDto created = await _addTicketCategoryCommandHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, created);
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        await _publishEventCommandHandler.HandleAsync(new PublishEventCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _cancelEventCommandHandler.HandleAsync(new CancelEventCommand(id), cancellationToken);
        return NoContent();
    }
}
