using Domain.Tickets;
using System;

namespace Application.Tickets.Queries.GetMyTickets;

public sealed record GetMyTicketsQuery(
    TicketStatus? Status = null,
    Guid? OrderId = null,
    int Page = 1,
    int PageSize = GetMyTicketsQuery.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
}
