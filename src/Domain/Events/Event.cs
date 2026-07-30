using Domain.Common;
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
    public EventStatus Status { get; private set; }
    public IReadOnlyCollection<TicketCategory> TicketCategories => new ReadOnlyCollection<TicketCategory>(_ticketCategories);

    private readonly List<TicketCategory> _ticketCategories = new();

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
            throw new InvalidOperationException("Only draft events can be put on sale.");

        Status = EventStatus.OnSale;
    }

    public void MarkAsSoldOut()
    {
        if (Status != EventStatus.OnSale)
            throw new InvalidOperationException("Only on-sale events can be marked as sold out.");

        Status = EventStatus.SoldOut;
    }

    public void Cancel()
    {
        if (Status is EventStatus.Completed or EventStatus.Cancelled)
            throw new InvalidOperationException("Event cannot be cancelled in its current state.");

        Status = EventStatus.Cancelled;
    }

    public void Complete()
    {
        if (Status == EventStatus.Cancelled)
            throw new InvalidOperationException("Cancelled events cannot be completed.");

        Status = EventStatus.Completed;
    }

    public TicketCategory AddTicketCategory(string name, Money price, int totalQuantity)
    {
        if (Status != EventStatus.Draft)
            throw new InvalidOperationException("Ticket categories can only be added while the event is in Draft status.");

        if (_ticketCategories.Any(tc => string.Equals(tc.Name, name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A ticket category with name '{name}' already exists for this event.");

        var category = new TicketCategory(Id, name, price, totalQuantity);
        _ticketCategories.Add(category);
        return category;
    }
}
