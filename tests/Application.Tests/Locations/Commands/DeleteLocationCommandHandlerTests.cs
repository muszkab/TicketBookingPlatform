using Application.Common.Exceptions;
using Application.Locations.Commands.DeleteLocation;
using Application.Tests.Common;
using System;
using System.Threading.Tasks;

namespace Application.Tests.Locations.Commands;

public class DeleteLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_LocationMissing()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var handler = new DeleteLocationCommandHandler(db);
        await FluentActions.Invoking(() => handler.HandleAsync(new DeleteLocationCommand(Guid.NewGuid())))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_Throw_Conflict_When_HasEvents()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewDraftEvent(location.Id);
        db.Locations.Add(location);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var handler = new DeleteLocationCommandHandler(db);
        await FluentActions.Invoking(() => handler.HandleAsync(new DeleteLocationCommand(location.Id)))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_Should_DeleteLocation()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation();
        db.Locations.Add(location);
        await db.SaveChangesAsync();

        var handler = new DeleteLocationCommandHandler(db);
        await handler.HandleAsync(new DeleteLocationCommand(location.Id));

        db.Locations.Should().BeEmpty();
    }
}
