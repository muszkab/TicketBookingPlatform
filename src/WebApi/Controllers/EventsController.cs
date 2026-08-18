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

namespace WebApi.Controllers;

[ApiVersion("1.0")]
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
    [HttpGet]
    [MapToApiVersion("1.0")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetAll(
        [FromQuery] EventCategory? category,
        [FromQuery] EventStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetEventsQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        PagedResult<EventDto> result = await getEventsQueryHandler.HandleAsync(new GetEventsQuery(category, status, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [MapToApiVersion("2.0")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetAllV2(
        [FromQuery] EventCategory? eventCategory,
        [FromQuery] EventStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetEventsQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        // TODO: v2-specific logic goes here (e.g. new filters, different DTO shape).
        PagedResult<EventDto> result = await getEventsQueryHandler.HandleAsync(new GetEventsQuery(eventCategory, status, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        EventDto? eventData = await getEventByIdQueryHandler.HandleAsync(new GetEventByIdQuery(id), cancellationToken);
        return eventData is null ? NotFound() : Ok(eventData);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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

        EventDto created = await createEventCommandHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEventRequest request, CancellationToken cancellationToken)
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

    [HttpPost("{id:guid}/ticket-categories")]
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

        return CreatedAtAction(nameof(GetById), new { id }, created);
    }

    [HttpDelete("{id:guid}/ticket-categories/{ticketCategoryId:guid}")]
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

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        await publishEventCommandHandler.HandleAsync(new PublishEventCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await cancelEventCommandHandler.HandleAsync(new CancelEventCommand(id), cancellationToken);
        return NoContent();
    }
}
