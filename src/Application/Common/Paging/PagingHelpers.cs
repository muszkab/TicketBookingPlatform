using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Paging;

internal static class PagingHelpers
{
    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize, int defaultPageSize)
    {
        int normalizedPage = Math.Max(page, 1);
        int normalizedPageSize = pageSize switch
        {
            < 1 => defaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize
        };
        return (normalizedPage, normalizedPageSize);
    }

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> source,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        int totalCount = await source.CountAsync(cancellationToken);
        List<T> items = await source
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}
