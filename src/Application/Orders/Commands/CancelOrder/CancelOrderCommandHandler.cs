using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Orders.Commands.CancelOrder;

public sealed class CancelOrderCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CancelOrderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<OrderDto> HandleAsync(CancelOrderCommand command, CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.UserId
            ?? throw new InvalidOperationException("Current user could not be determined.");

        Order? order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.TicketCategory)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

        if (order is null || order.UserId != userId)
            throw new NotFoundException(nameof(Order), command.OrderId);

        order.Cancel();
        await _context.SaveChangesAsync(cancellationToken);

        return OrderMapper.ToDto(order);
    }
}
