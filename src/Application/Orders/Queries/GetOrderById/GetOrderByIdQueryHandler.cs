using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;
using System;
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
            .ThenInclude(i => i.TicketCategory)
            .FirstOrDefaultAsync(o => o.Id == query.OrderId, cancellationToken);

        if (order is null || order.UserId != userId)
            return null;

        return OrderMapper.ToDto(order);
    }
}
