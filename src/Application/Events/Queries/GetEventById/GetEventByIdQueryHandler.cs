using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events.Queries.GetEventById;

public sealed class GetEventByIdQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetEventByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EventDto?> HandleAsync(
        GetEventByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _context.Events
            .AsNoTracking()
            .Where(e => e.Id == query.Id)
            .Select(e => new EventDto(
                e.Id,
                e.Title,
                e.Description,
                e.Category,
                e.Status,
                e.StartsAt,
                e.EndsAt,
                e.LocationId,
                e.TicketCategories
                    .Select(tc => new TicketCategoryDto(
                        tc.Id,
                        tc.Name,
                        tc.Price.Amount,
                        tc.Price.Currency,
                        tc.TotalQuantity,
                        tc.AvailableQuantity))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
