using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Events;
using Domain.Orders;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events.Commands.CancelEvent;

public sealed class CancelEventCommandHandler
{
    private readonly IApplicationDbContext _context;

    public CancelEventCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task HandleAsync(CancelEventCommand command, CancellationToken cancellationToken = default)
    {
        var targetEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), command.Id);

        targetEvent.Cancel();

        List<Order> pendingOrders = await _context.Orders
            .Where(o => o.EventId == command.Id && o.Status == OrderStatus.Pending)
            .ToListAsync(cancellationToken);

        foreach (Order order in pendingOrders)
            order.Cancel();

        List<Ticket> validTickets = await _context.Tickets
            .Where(t => t.EventId == command.Id && t.Status == TicketStatus.Valid)
            .ToListAsync(cancellationToken);

        foreach (Ticket ticket in validTickets)
            ticket.Cancel();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
