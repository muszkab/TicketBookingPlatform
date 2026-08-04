using Domain.Common;
using System;

namespace Domain.Orders;

public class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid TicketCategoryId { get; private set; }
    public int Quantity { get; private set; }
    public Money UnitPrice { get; private set; }

    public Money LineTotal => UnitPrice.Multiply(Quantity);

    private OrderItem()
    {
        UnitPrice = null!;
    }

    internal OrderItem(Guid orderId, Guid ticketCategoryId, int quantity, Money unitPrice)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("OrderId is required.", nameof(orderId));

        if (ticketCategoryId == Guid.Empty)
            throw new ArgumentException("TicketCategoryId is required.", nameof(ticketCategoryId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        ArgumentNullException.ThrowIfNull(unitPrice);

        OrderId = orderId;
        TicketCategoryId = ticketCategoryId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
