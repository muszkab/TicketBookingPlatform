using Domain.Common;
using System;

namespace Domain.Events;

public class Event : Entity
{
    public string Title { get; private set; }
    public string Description { get; private set; }
    public EventCategory Category { get; private set; }
    public DateTimeOffset StartsAt { get; private set; }
    public DateTimeOffset EndsAt { get; private set; }
    public Guid VenueId { get; private set; }
    public EventStatus Status { get; private set; }

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
        Guid venueId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        if (endsAt <= startsAt)
            throw new ArgumentException("EndsAt must be after StartsAt.", nameof(endsAt));

        if (venueId == Guid.Empty)
            throw new ArgumentException("VenueId is required.", nameof(venueId));

        Title = title;
        Description = description ?? string.Empty;
        Category = category;
        StartsAt = startsAt;
        EndsAt = endsAt;
        VenueId = venueId;
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
}
