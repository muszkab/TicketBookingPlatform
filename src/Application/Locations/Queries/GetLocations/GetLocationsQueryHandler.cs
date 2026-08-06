using Application.Common.Interfaces;
using Application.Common.Paging;
using Microsoft.EntityFrameworkCore;
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

    public async Task<PagedResult<LocationDto>> HandleAsync(
        GetLocationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = PagingHelpers.Normalize(query.Page, query.PageSize, GetLocationsQuery.DefaultPageSize);

        var source = _context.Locations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var name = query.Name.Trim();
            source = source.Where(l => EF.Functions.Like(l.Name, $"%{name}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            source = source.Where(l => EF.Functions.Like(l.City, $"%{city}%"));
        }

        return await source
            .OrderBy(l => l.Name)
            .Select(l => new LocationDto(
                l.Id,
                l.Name,
                l.Street,
                l.City,
                l.PostalCode,
                l.Country,
                l.Capacity))
            .ToPagedResultAsync(page, pageSize, cancellationToken);
    }
}
