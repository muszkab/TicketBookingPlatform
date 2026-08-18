using Application.Common.Exceptions;
using Application.Events.Commands.UpdateEvent;
using Application.Tests.Common;
using Domain.Events;
using System;
using System.Threading.Tasks;

namespace Application.Tests.Events.Commands;

public class UpdateEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_EventMissing()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var handler = new UpdateEventCommandHandler(db);
        var command = new UpdateEventCommand(Guid.NewGuid(), "t", "d", EventCategory.Concert,
            DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(2));

        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_UpdateEvent()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewDraftEvent(location.Id);
        db.Locations.Add(location);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var start = DateTimeOffset.UtcNow.AddDays(20);
        var end = start.AddHours(4);
        var handler = new UpdateEventCommandHandler(db);
        await handler.HandleAsync(new UpdateEventCommand(ev.Id, "New", "NewDesc", EventCategory.Theater, start, end));

        ev.Title.Should().Be("New");
        ev.Category.Should().Be(EventCategory.Theater);
        ev.StartsAt.Should().Be(start);
        ev.EndsAt.Should().Be(end);
    }
}
