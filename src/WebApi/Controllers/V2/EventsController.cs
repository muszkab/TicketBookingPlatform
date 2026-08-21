using Application.Common.Paging;
using Application.Events;
using Application.Events.Commands.AddTicketCategory;
using Application.Events.Commands.CancelEvent;
using Application.Events.Commands.CreateEvent;
using Application.Events.Commands.PublishEvent;
using Application.Events.Commands.RemoveTicketCategory;
using Application.Events.Commands.UpdateEvent;
using Application.Events.Queries.GetEventById;
using Application.Events.Queries.GetEvents;
using Asp.Versioning;
using Domain.Events;
using Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Contracts.Events;

namespace WebApi.Controllers.V2;

[ApiVersion("2.0")]
[Authorize(Roles = $"{nameof(UserRole.Organizer)},{nameof(UserRole.Admin)}")]
public class EventsController(
    GetEventsQueryHandler getEventsQueryHandler,
    GetEventByIdQueryHandler getEventByIdQueryHandler,
    CreateEventCommandHandler createEventCommandHandler,
    UpdateEventCommandHandler updateEventCommandHandler,
    AddTicketCategoryCommandHandler addTicketCategoryCommandHandler,
    RemoveTicketCategoryCommandHandler removeTicketCategoryCommandHandler,
    PublishEventCommandHandler publishEventCommandHandler,
    CancelEventCommandHandler cancelEventCommandHandler)
    : ApiControllerBase
{
    [HttpGet(Name = nameof(GetEvents))]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetEvents(
        [FromQuery] EventCategory? category,
        [FromQuery] EventStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetEventsQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        PagedResult<EventDto> result = await getEventsQueryHandler.HandleAsync(new GetEventsQuery(category, status, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = nameof(GetEventById))]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetEventById(Guid id, CancellationToken cancellationToken)
    {
        EventDto? eventData = await getEventByIdQueryHandler.HandleAsync(new GetEventByIdQuery(id), cancellationToken);
        return eventData is null ? NotFound() : Ok(eventData);
    }

    [HttpPost(Name = nameof(CreateEvent))]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateEventCommand(
            request.Title,
            request.Description,
            request.Category,
            request.StartsAt,
            request.EndsAt,
            request.LocationId);

        EventDto created = await createEventCommandHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetEventById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}", Name = nameof(UpdateEvent))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateEventCommand(
            id,
            request.Title,
            request.Description,
            request.Category,
            request.StartsAt,
            request.EndsAt);

        await updateEventCommandHandler.HandleAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/ticket-categories", Name = nameof(AddTicketCategory))]
    [ProducesResponseType(typeof(TicketCategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TicketCategoryDto>> AddTicketCategory(Guid id, [FromBody] AddTicketCategoryRequest request, CancellationToken cancellationToken)
    {
        var command = new AddTicketCategoryCommand(
            id,
            request.Name,
            request.Price,
            request.Currency,
            request.Quantity);

        TicketCategoryDto created = await addTicketCategoryCommandHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetEventById), new { id }, created);
    }

    [HttpDelete("{id:guid}/ticket-categories/{ticketCategoryId:guid}", Name = nameof(RemoveTicketCategory))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveTicketCategory(Guid id, Guid ticketCategoryId, CancellationToken cancellationToken)
    {
        await removeTicketCategoryCommandHandler.HandleAsync(new RemoveTicketCategoryCommand(id, ticketCategoryId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/publish", Name = nameof(PublishEvent))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PublishEvent(Guid id, CancellationToken cancellationToken)
    {
        await publishEventCommandHandler.HandleAsync(new PublishEventCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel", Name = nameof(CancelEvent))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelEvent(Guid id, CancellationToken cancellationToken)
    {
        await cancelEventCommandHandler.HandleAsync(new CancelEventCommand(id), cancellationToken);
        return NoContent();
    }
}
