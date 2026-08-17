using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Orders.Queries.GetOrderById;

public sealed class GetOrderByIdQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetOrderByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<OrderDto?> HandleAsync(GetOrderByIdQuery query, CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.UserId
            ?? throw new NotFoundException(nameof(Order), query.OrderId);

        Order? order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == query.OrderId, cancellationToken);

        if (order is null || order.UserId != userId)
            return null;

        var categoryIds = order.Items.Select(i => i.TicketCategoryId).ToList();
        var categoryNames = await _context.TicketCategories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return new OrderDto(
            order.Id,
            order.UserId,
            order.EventId,
            order.Status,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.CreatedAt,
            order.PaidAt,
            order.CancelledAt,
            order.Items
                .Select(i => new OrderItemDto(
                    i.Id,
                    i.TicketCategoryId,
                    categoryNames.TryGetValue(i.TicketCategoryId, out string? name) ? name : string.Empty,
                    i.Quantity,
                    i.UnitPrice.Amount,
                    i.UnitPrice.Currency,
                    i.LineTotal.Amount))
                .ToList());
    }
}
