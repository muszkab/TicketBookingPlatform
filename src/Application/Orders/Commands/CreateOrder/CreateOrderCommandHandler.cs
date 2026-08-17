using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Events;
using Domain.Orders;
using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Orders.Commands.CreateOrder;

public sealed class CreateOrderCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateOrderCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<OrderDto> HandleAsync(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        Guid userId = _currentUser.UserId
            ?? throw new ConflictException("Current user could not be determined.");

        if (command.Items is null || command.Items.Count == 0)
            throw new ArgumentException("Order must contain at least one item.", nameof(command));

        Event targetEvent = await _context.Events
            .Include(e => e.TicketCategories)
            .FirstOrDefaultAsync(e => e.Id == command.EventId, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), command.EventId);

        if (targetEvent.Status != EventStatus.OnSale)
            throw new ConflictException($"Event is not on sale (current status: {targetEvent.Status}).");

        Dictionary<Guid, TicketCategory> categoriesById = targetEvent.TicketCategories.ToDictionary(c => c.Id);

        var order = new Order(userId, targetEvent.Id, command.Currency);

        foreach (CreateOrderItemDto item in command.Items)
        {
            if (!categoriesById.TryGetValue(item.TicketCategoryId, out TicketCategory? category))
                throw new NotFoundException(nameof(TicketCategory), item.TicketCategoryId);

            order.AddItem(category, item.Quantity);
        }

        IReadOnlyList<Ticket> tickets = order.Pay(categoriesById);

        _context.Orders.Add(order);
        _context.Tickets.AddRange(tickets);
        await _context.SaveChangesAsync(cancellationToken);

        List<OrderItemDto> itemDtos = order.Items
            .Select(i => MapToItemDto(i, categoriesById[i.TicketCategoryId]))
            .ToList();

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
            itemDtos);
    }

    private static OrderItemDto MapToItemDto(OrderItem item, TicketCategory category)
    {
        return new OrderItemDto(
            item.Id,
            item.TicketCategoryId,
            category.Name,
            item.Quantity,
            item.UnitPrice.Amount,
            item.UnitPrice.Currency,
            item.LineTotal.Amount);
    }
}
