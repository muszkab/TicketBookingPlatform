using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Events;
using Domain.Orders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
            ?? throw new NotFoundException(nameof(Order), command.OrderId);

        Order? order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

        if (order is null || order.UserId != userId)
            throw new NotFoundException(nameof(Order), command.OrderId);

        order.Cancel();
        await _context.SaveChangesAsync(cancellationToken);

        List<Guid> categoryIds = order.Items.Select(i => i.TicketCategoryId).ToList();
        Dictionary<Guid, TicketCategory> categoriesById = await _context.TicketCategories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        return OrderMapper.ToDto(order, categoriesById);
    }
}
