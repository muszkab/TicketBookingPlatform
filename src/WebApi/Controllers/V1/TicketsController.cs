using Application.Common.Paging;
using Application.Tickets;
using Application.Tickets.Queries.GetMyTickets;
using Application.Tickets.Queries.GetTicketById;
using Asp.Versioning;
using Domain.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebApi.Controllers.V1;

[ApiVersion("1.0")]
[Authorize]
public class TicketsController(
    GetMyTicketsQueryHandler getMyTicketsQueryHandler,
    GetTicketByIdQueryHandler getTicketByIdQueryHandler)
    : ApiControllerBase
{
    [HttpGet("{id:guid}", Name = nameof(GetTicketById))]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> GetTicketById(Guid id, CancellationToken cancellationToken)
    {
        TicketDto? ticket = await getTicketByIdQueryHandler.HandleAsync(new GetTicketByIdQuery(id), cancellationToken);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpGet("mine", Name = nameof(GetMyTickets))]
    [ProducesResponseType(typeof(PagedResult<TicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PagedResult<TicketDto>>> GetMyTickets(
        [FromQuery] TicketStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetMyTicketsQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        PagedResult<TicketDto> result = await getMyTicketsQueryHandler.HandleAsync(new GetMyTicketsQuery(status, page, pageSize), cancellationToken);
        return Ok(result);
    }
}
