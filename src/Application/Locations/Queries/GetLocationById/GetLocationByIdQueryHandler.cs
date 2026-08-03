using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Locations.Queries.GetLocationById;

public sealed class GetLocationByIdQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetLocationByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LocationDto?> HandleAsync(
        GetLocationByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _context.Locations
            .AsNoTracking()
            .Where(l => l.Id == query.Id)
            .Select(l => new LocationDto(
                l.Id,
                l.Name,
                l.Street,
                l.City,
                l.PostalCode,
                l.Country,
                l.Capacity))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
