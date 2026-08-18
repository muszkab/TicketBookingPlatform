using Application.Common.Exceptions;
using Application.Events.Commands.CreateEvent;
using Application.Tests.Common;
using Domain.Events;
using System;
using System.Threading.Tasks;

namespace Application.Tests.Events.Commands;

public class CreateEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_LocationMissing()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var handler = new CreateEventCommandHandler(db);
        var command = new CreateEventCommand("Show", "d", EventCategory.Concert,
            DateTimeOffset.UtcNow.AddDays(1), DateTimeOffset.UtcNow.AddDays(1).AddHours(2), Guid.NewGuid());

        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_PersistEvent_And_ReturnDto()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation();
        db.Locations.Add(location);
        await db.SaveChangesAsync();

        var handler = new CreateEventCommandHandler(db);
        var start = DateTimeOffset.UtcNow.AddDays(5);
        var command = new CreateEventCommand("Show", "desc", EventCategory.Theater, start, start.AddHours(2), location.Id);

        var dto = await handler.HandleAsync(command);

        dto.Title.Should().Be("Show");
        dto.Status.Should().Be(EventStatus.Draft);
        dto.LocationId.Should().Be(location.Id);
        db.Events.Should().HaveCount(1);
    }
}
