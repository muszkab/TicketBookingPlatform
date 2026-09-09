using Application.Common.Interfaces;
using Application.Common.Paging;
using Domain.Orders;
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
            ?? throw new InvalidOperationException("Current user could not be determined.");

        (int page, int pageSize) = PagingHelpers.Normalize(query.Page, query.PageSize, GetMyOrdersQuery.DefaultPageSize);

        IQueryable<Order> ordered = ApplySort(
            _context.Orders.AsNoTracking().Where(o => o.UserId == userId),
            query.SortBy,
            query.SortDir);

        IQueryable<OrderSummaryDto> source = ordered
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.EventId,
                o.Event!.Title,
                o.Status,
                o.TotalAmount.Amount,
                o.TotalAmount.Currency,
                o.Items.Count,
                o.Items.Sum(i => i.Quantity),
                o.CreatedAt));

        return await source.ToPagedResultAsync(page, pageSize, cancellationToken);
    }

    private static IQueryable<Order> ApplySort(
        IQueryable<Order> source,
        OrderSortField sortBy,
        SortDirection sortDir)
    {
        bool desc = sortDir == SortDirection.Desc;

        IOrderedQueryable<Order> ordered = sortBy switch
        {
            OrderSortField.TotalAmount => desc
                ? source.OrderByDescending(o => o.TotalAmount.Amount)
                : source.OrderBy(o => o.TotalAmount.Amount),
            OrderSortField.Status => desc
                ? source.OrderByDescending(o => o.Status)
                : source.OrderBy(o => o.Status),
            OrderSortField.EventTitle => desc
                ? source.OrderByDescending(o => o.Event!.Title)
                : source.OrderBy(o => o.Event!.Title),
            OrderSortField.TicketQuantity => desc
                ? source.OrderByDescending(o => o.Items.Sum(i => i.Quantity))
                : source.OrderBy(o => o.Items.Sum(i => i.Quantity)),
            _ => desc
                ? source.OrderByDescending(o => o.CreatedAt)
                : source.OrderBy(o => o.CreatedAt)
        };

        return ordered.ThenBy(o => o.Id);
    }
}
