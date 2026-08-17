using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Tickets.Queries.GetTicketById;

public sealed class GetTicketByIdQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetTicketByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<TicketDto?> HandleAsync(GetTicketByIdQuery query, CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.UserId
            ?? throw new InvalidOperationException("Current user could not be determined.");

        return await _context.Tickets
            .AsNoTracking()
            .Where(t => t.Id == query.TicketId && t.Order.UserId == userId)
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
                t.UsedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
