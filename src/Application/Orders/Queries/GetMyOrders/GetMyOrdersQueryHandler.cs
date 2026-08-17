using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Common.Paging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Orders.Queries.GetMyOrders;

public sealed class GetMyOrdersQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetMyOrdersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<OrderSummaryDto>> HandleAsync(GetMyOrdersQuery query, CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.UserId
            ?? throw new ConflictException("Current user could not be determined.");

        (int page, int pageSize) = PagingHelpers.Normalize(query.Page, query.PageSize, GetMyOrdersQuery.DefaultPageSize);

        IQueryable<OrderSummaryDto> source = _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.EventId,
                o.Status,
                o.TotalAmount.Amount,
                o.TotalAmount.Currency,
                o.Items.Count,
                o.CreatedAt));

        return await source.ToPagedResultAsync(page, pageSize, cancellationToken);
    }
}
