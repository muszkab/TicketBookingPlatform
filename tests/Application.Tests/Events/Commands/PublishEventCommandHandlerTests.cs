using Application.Common.Exceptions;
using Application.Events.Commands.PublishEvent;
using Application.Tests.Common;
using Domain.Events;
using System;
using System.Threading.Tasks;

namespace Application.Tests.Events.Commands;

public class PublishEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_EventMissing()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var handler = new PublishEventCommandHandler(db);

        await FluentActions.Invoking(() => handler.HandleAsync(new PublishEventCommand(Guid.NewGuid())))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_TransitionEvent_To_OnSale()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewDraftEvent(location.Id);
        db.Locations.Add(location);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var handler = new PublishEventCommandHandler(db);
        await handler.HandleAsync(new PublishEventCommand(ev.Id));

        ev.Status.Should().Be(EventStatus.OnSale);
    }
}
