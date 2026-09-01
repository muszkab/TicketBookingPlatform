using Domain.Common;
using Domain.Common.Exceptions;
using System;

namespace Domain.Events;

public class TicketCategory : Entity
{
    public Guid EventId { get; private set; }
    public string Name { get; private set; }
    public Money Price { get; private set; }
    public int TotalQuantity { get; private set; }
    public int AvailableQuantity { get; private set; }

    private TicketCategory()
    {
        Name = string.Empty;
        Price = null!;
    }

    internal TicketCategory(Guid eventId, string name, Money price, int totalQuantity)
        : base(CreateId())
    {
        if (eventId == Guid.Empty)
            throw new ArgumentException("EventId is required.", nameof(eventId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (price is null)
            throw new ArgumentNullException(nameof(price));

        if (totalQuantity <= 0)
            throw new ArgumentException("TotalQuantity must be greater than zero.", nameof(totalQuantity));

        EventId = eventId;
        Name = name;
        Price = price;
        TotalQuantity = totalQuantity;
        AvailableQuantity = totalQuantity;
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        if (quantity > AvailableQuantity)
            throw new BusinessRuleException($"Not enough tickets available in category '{Name}'. Requested {quantity}, available {AvailableQuantity}.");

        AvailableQuantity -= quantity;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        if (AvailableQuantity + quantity > TotalQuantity)
            throw new BusinessRuleException("Cannot release more tickets than were reserved.");

        AvailableQuantity += quantity;
    }
}
