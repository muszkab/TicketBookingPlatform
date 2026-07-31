using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Locations.Queries.GetLocations;

public sealed class GetLocationsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetLocationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LocationDto>> HandleAsync(
        GetLocationsQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _context.Locations
            .AsNoTracking()
            .OrderBy(l => l.Name)
            .Select(l => new LocationDto(
                l.Id,
                l.Name,
                l.Street,
                l.City,
                l.PostalCode,
                l.Country,
                l.Capacity))
            .ToListAsync(cancellationToken);
    }
}
