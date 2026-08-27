using Application.Common.Interfaces;
using Application.Common.Paging;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events.Queries.GetEvents;

public sealed class GetEventsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetEventsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<EventDto>> HandleAsync(
        GetEventsQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = PagingHelpers.Normalize(query.Page, query.PageSize, GetEventsQuery.DefaultPageSize);

        var source = _context.Events.AsNoTracking();

        if (query.Category.HasValue)
            source = source.Where(e => e.Category == query.Category.Value);

        if (query.Status.HasValue)
            source = source.Where(e => e.Status == query.Status.Value);

        if (query.LocationId.HasValue)
            source = source.Where(e => e.LocationId == query.LocationId.Value);

        return await source
            .OrderBy(e => e.StartsAt)
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
            .ToPagedResultAsync(page, pageSize, cancellationToken);
    }
}
