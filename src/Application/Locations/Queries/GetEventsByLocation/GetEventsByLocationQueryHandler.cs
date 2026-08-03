using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Locations.Queries.GetEventsByLocation;

public sealed class GetEventsByLocationQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetEventsByLocationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LocationEventDto>?> HandleAsync(
        GetEventsByLocationQuery query,
        CancellationToken cancellationToken = default)
    {
        var locationExists = await _context.Locations
            .AsNoTracking()
            .AnyAsync(l => l.Id == query.LocationId, cancellationToken);

        if (!locationExists)
            return null;

        return await _context.Events
            .AsNoTracking()
            .Where(e => e.LocationId == query.LocationId)
            .OrderBy(e => e.StartsAt)
            .Select(e => new LocationEventDto(
                e.Id,
                e.Title,
                e.Description,
                e.Category,
                e.StartsAt,
                e.EndsAt,
                e.Status))
            .ToListAsync(cancellationToken);
    }
}
