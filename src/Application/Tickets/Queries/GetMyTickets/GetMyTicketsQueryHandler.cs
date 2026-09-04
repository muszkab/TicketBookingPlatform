using Application.Common.Interfaces;
using Application.Common.Paging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Tickets.Queries.GetMyTickets;

public sealed class GetMyTicketsQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyTicketsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<TicketDto>> HandleAsync(GetMyTicketsQuery query, CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.UserId
            ?? throw new InvalidOperationException("Current user could not be determined.");

        var (page, pageSize) = PagingHelpers.Normalize(query.Page, query.PageSize, GetMyTicketsQuery.DefaultPageSize);

        var source = _context.Tickets
            .AsNoTracking()
            .Where(t => t.Order.UserId == userId);

        if (query.Status.HasValue)
            source = source.Where(t => t.Status == query.Status.Value);

        if (query.OrderId.HasValue)
            source = source.Where(t => t.OrderId == query.OrderId.Value);

        var projected = source
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TicketDto(
                t.Id,
                t.OrderId,
                t.EventId,
                t.Event.Title,
                t.TicketCategoryId,
                t.TicketCategory.Name,
                t.Code,
                t.Status,
                t.CreatedAt,
                t.UsedAt));

        return await projected.ToPagedResultAsync(page, pageSize, cancellationToken);
    }
}
