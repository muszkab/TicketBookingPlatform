using Domain.Common;
using Domain.Events;
using Domain.Tickets;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Domain.Orders;

public class Order : Entity
{
    public Guid UserId { get; private set; }
    public Guid EventId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => new ReadOnlyCollection<OrderItem>(_items);

    private Order()
    {
        TotalAmount = null!;
    }

    public Order(Guid userId, Guid eventId, string currency)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId is required.", nameof(userId));

        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId is required.", nameof(eventId));

        UserId = userId;
        EventId = eventId;
        Status = OrderStatus.Pending;
        TotalAmount = Money.Zero(currency);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public OrderItem AddItem(TicketCategory category, int quantity)
    {
        EnsurePending();
        ArgumentNullException.ThrowIfNull(category);

        if (category.EventId != EventId)
            throw new InvalidOperationException("Ticket category does not belong to this event.");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        if (_items.Any(i => i.TicketCategoryId == category.Id))
            throw new InvalidOperationException("This ticket category is already in the order.");

        var item = new OrderItem(Id, category.Id, quantity, category.Price);
        _items.Add(item);
        TotalAmount = TotalAmount.Add(item.LineTotal);
        return item;
    }

    public IReadOnlyList<Ticket> Pay(IReadOnlyDictionary<Guid, TicketCategory> categoriesById)
    {
        EnsurePending();

        if (_items.Count == 0)
            throw new InvalidOperationException("Cannot pay for an order without items.");

        ArgumentNullException.ThrowIfNull(categoriesById);

        foreach (OrderItem item in _items)
        {
            if (!categoriesById.TryGetValue(item.TicketCategoryId, out TicketCategory? category))
                throw new InvalidOperationException($"Ticket category {item.TicketCategoryId} not provided.");

            category.Reserve(item.Quantity);
        }

        var tickets = new List<Ticket>();
        foreach (OrderItem item in _items)
        {
            for (int i = 0; i < item.Quantity; i++)
            {
                tickets.Add(new Ticket(Id, item.Id, EventId, item.TicketCategoryId));
            }
        }

        Status = OrderStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;
        return tickets;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            return;

        if (Status == OrderStatus.Paid)
            throw new InvalidOperationException("Paid orders cannot be cancelled through this method.");

        Status = OrderStatus.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
    }

    private void EnsurePending()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Operation not allowed on order in status {Status}.");
    }
}
