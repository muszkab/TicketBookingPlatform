using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Orders;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Orders.Commands.PayOrder;

public sealed class PayOrderCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public PayOrderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<OrderDto> HandleAsync(PayOrderCommand command, CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.UserId
            ?? throw new InvalidOperationException("Current user could not be determined.");

        Order? order = await _context.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.TicketCategory)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

        if (order is null || order.UserId != userId)
            throw new NotFoundException(nameof(Order), command.OrderId);

        IReadOnlyList<Ticket> tickets = order.Pay();

        _context.Tickets.AddRange(tickets);
        await _context.SaveChangesAsync(cancellationToken);

        return OrderMapper.ToDto(order);
    }
}
