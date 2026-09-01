using Domain.Common;
using Domain.Events;
using System;

namespace Domain.Orders;

public class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid TicketCategoryId { get; private set; }
    public TicketCategory TicketCategory { get; private set; } = null!;
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private OrderItem() : base(EmptyId)
    {
        UnitPrice = null!;
    }

    internal OrderItem(Guid orderId, TicketCategory ticketCategory, int quantity, Money unitPrice)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId is required.", nameof(orderId));

        ArgumentNullException.ThrowIfNull(ticketCategory);

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        ArgumentNullException.ThrowIfNull(unitPrice);

        OrderId = orderId;
        TicketCategoryId = ticketCategory.Id;
        TicketCategory = ticketCategory;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
