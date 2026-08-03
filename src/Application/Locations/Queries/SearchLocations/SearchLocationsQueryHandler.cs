using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Locations.Queries.GetLocations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Locations.Queries.SearchLocations;

public sealed class SearchLocationsQueryHandler
{
    private const int MaxPageSize = 100;

    private readonly IApplicationDbContext _context;

    public SearchLocationsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<LocationDto>> HandleAsync(
        SearchLocationsQuery query,
        CancellationToken cancellationToken = default)
    {
        int page = Math.Max(query.Page, 1);

        int pageSize = query.PageSize switch
        {
            < 1 => SearchLocationsQuery.DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => query.PageSize
        };

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

        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .OrderBy(l => l.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LocationDto(
                l.Id,
                l.Name,
                l.Street,
                l.City,
                l.PostalCode,
                l.Country,
                l.Capacity))
            .ToListAsync(cancellationToken);

        return new PagedResult<LocationDto>(items, page, pageSize, totalCount);
    }
}
