using Domain.Common;
using Domain.Events;
using Domain.Orders;
using System;

namespace Domain.Tickets;

public class Ticket : Entity
{
    public Guid OrderId { get; private set; }
    public Order Order { get; private set; } = null!;
    public Guid OrderItemId { get; private set; }
    public Guid EventId { get; private set; }
    public Event Event { get; private set; } = null!;
    public Guid TicketCategoryId { get; private set; }
    public TicketCategory TicketCategory { get; private set; } = null!;
    public string Code { get; private set; }
    public TicketStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    private Ticket()
    {
        Code = string.Empty;
    }

    internal Ticket(Guid orderId, Guid orderItemId, Guid eventId, Guid ticketCategoryId)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId is required.", nameof(orderId));
        if (orderItemId == Guid.Empty)
            throw new ArgumentException("OrderItemId is required.", nameof(orderItemId));
        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId is required.", nameof(eventId));
        if (ticketCategoryId == Guid.Empty)
            throw new ArgumentException("TicketCategoryId is required.", nameof(ticketCategoryId));

        OrderId = orderId;
        OrderItemId = orderItemId;
        EventId = eventId;
        TicketCategoryId = ticketCategoryId;
        Code = TicketCode.New();
        Status = TicketStatus.Valid;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsUsed()
    {
        if (Status != TicketStatus.Valid)
            throw new InvalidOperationException($"Only valid tickets can be used. Current status: {Status}.");

        Status = TicketStatus.Used;
        UsedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == TicketStatus.Used)
            throw new InvalidOperationException("Used tickets cannot be cancelled.");

        Status = TicketStatus.Cancelled;
    }
}
