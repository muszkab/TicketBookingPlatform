using Application.Locations.Queries.GetEventsByLocation;
using Application.Tests.Common;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Tests.Locations.Queries;

public class GetEventsByLocationQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnNull_When_LocationMissing()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var handler = new GetEventsByLocationQueryHandler(db);
        var result = await handler.HandleAsync(new GetEventsByLocationQuery(Guid.NewGuid()));
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnEmpty_When_LocationHasNoEvents()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation();
        db.Locations.Add(location);
        await db.SaveChangesAsync();

        var handler = new GetEventsByLocationQueryHandler(db);
        var result = await handler.HandleAsync(new GetEventsByLocationQuery(location.Id));

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_ReturnEventsOfLocation_OrderedByStartsAt()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation();
        var otherLocation = DomainFactory.NewLocation();

        var eventLater = DomainFactory.NewDraftEvent(location.Id, "Later");
        var eventEarlier = new Domain.Events.Event(
            "Earlier", "d", Domain.Events.EventCategory.Concert,
            DateTimeOffset.UtcNow.AddDays(1),
            DateTimeOffset.UtcNow.AddDays(1).AddHours(2),
            location.Id);
        var eventInOtherLocation = DomainFactory.NewDraftEvent(otherLocation.Id, "Other");

        db.Locations.AddRange(location, otherLocation);
        db.Events.AddRange(eventLater, eventEarlier, eventInOtherLocation);
        await db.SaveChangesAsync();

        var handler = new GetEventsByLocationQueryHandler(db);
        var result = await handler.HandleAsync(new GetEventsByLocationQuery(location.Id));

        result.Should().NotBeNull();
        result!.Select(e => e.Title).Should().ContainInOrder("Earlier", "Later");
        result.Should().NotContain(e => e.Title == "Other");
    }
}
