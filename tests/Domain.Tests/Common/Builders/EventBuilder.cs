using Domain.Events;
using System;

namespace Domain.Tests.Common.Builders;

internal sealed class EventBuilder
{
    private string _title = "Test Event";
    private string _description = "Description";
    private EventCategory _category = EventCategory.Concert;
    private DateTimeOffset _startsAt = DateTimeOffset.UtcNow.AddDays(1);
    private DateTimeOffset _endsAt = DateTimeOffset.UtcNow.AddDays(1).AddHours(2);
    private Guid _locationId = Guid.NewGuid();

    public EventBuilder WithTitle(string title) { _title = title; return this; }
    public EventBuilder WithSchedule(DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        _startsAt = startsAt;
        _endsAt = endsAt;
        return this;
    }
    public EventBuilder WithLocation(Guid locationId) { _locationId = locationId; return this; }

    public Event Build()
        => new(_title, _description, _category, _startsAt, _endsAt, _locationId);

    public Event BuildOnSale(int locationCapacity = 100)
    {
        var ev = Build();
        ev.AddTicketCategory("Standard", new Domain.Common.Money(10m, "EUR"), 10, locationCapacity);
        ev.PutOnSale();
        return ev;
    }
}
