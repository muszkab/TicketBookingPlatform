using Domain.Tickets;

namespace Application.Tickets.Queries.GetMyTickets;

public sealed record GetMyTicketsQuery(
    TicketStatus? Status = null,
    int Page = 1,
    int PageSize = GetMyTicketsQuery.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
}
