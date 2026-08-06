using Domain.Common;
using Domain.Common.Exceptions;
using Domain.Locations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Domain.Events;

public class Event : Entity
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public EventCategory Category { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public Guid LocationId { get; private set; }
    public Location? Location { get; private set; }
    public EventStatus Status { get; private set; }

    private readonly List<TicketCategory> _ticketCategories = new();
    public IReadOnlyCollection<TicketCategory> TicketCategories => new ReadOnlyCollection<TicketCategory>(_ticketCategories);

    private Event()
    {
        Title = string.Empty;
        Description = string.Empty;
    }

    public Event(
        string title,
        string description,
        EventCategory category,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        Guid locationId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (endsAt <= startsAt)
            throw new ArgumentException("EndsAt must be after StartsAt.", nameof(endsAt));

        if (locationId == Guid.Empty)
            throw new ArgumentException("LocationId is required.", nameof(locationId));

        Title = title;
        Description = description ?? string.Empty;
        Category = category;
        StartsAt = startsAt;
        EndsAt = endsAt;
        LocationId = locationId;
        Status = EventStatus.Draft;
    }

    public void PutOnSale()
    {
        if (Status != EventStatus.Draft)
            throw new BusinessRuleException("Only draft events can be put on sale.");

        Status = EventStatus.OnSale;
    }

    public void MarkAsSoldOut()
    {
        if (Status != EventStatus.OnSale)
            throw new BusinessRuleException("Only on-sale events can be marked as sold out.");

        Status = EventStatus.SoldOut;
    }

    public void Cancel()
    {
        if (Status is EventStatus.Completed or EventStatus.Cancelled)
            throw new BusinessRuleException("Event cannot be cancelled in its current state.");

        Status = EventStatus.Cancelled;
    }

    public void Complete()
    {
        if (Status == EventStatus.Cancelled)
            throw new BusinessRuleException("Cancelled events cannot be completed.");

        Status = EventStatus.Completed;
    }

    public TicketCategory AddTicketCategory(string name, Money price, int categoryQuantity, int locationCapacity)
    {
        if (Status != EventStatus.Draft)
            throw new BusinessRuleException("Ticket categories can only be added while the event is in Draft status.");

        if (_ticketCategories.Any(tc => string.Equals(tc.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessRuleException($"A ticket category with name '{name}' already exists for this event.");

        if (locationCapacity <= 0)
            throw new ArgumentException("Location capacity must be greater than zero.", nameof(locationCapacity));

        int currentAllocated = _ticketCategories.Sum(tc => tc.TotalQuantity);
        if (currentAllocated + categoryQuantity > locationCapacity)
        {
            throw new BusinessRuleException(
                $"Adding {categoryQuantity} tickets in category '{name}' would exceed the location capacity ({locationCapacity}). Currently allocated: {currentAllocated}.");
        }

        var category = new TicketCategory(Id, name, price, categoryQuantity);
        _ticketCategories.Add(category);
        return category;
    }
}
