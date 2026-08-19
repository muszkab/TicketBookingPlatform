using Application.Common.Exceptions;
using Application.Locations.Commands.UpdateLocation;
using Application.Tests.Common;
using System;
using System.Threading.Tasks;

namespace Application.Tests.Locations.Commands;

public class UpdateLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_LocationMissing()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var handler = new UpdateLocationCommandHandler(db);
        await FluentActions.Invoking(() => handler.HandleAsync(
                new UpdateLocationCommand(Guid.NewGuid(), "N", "s", "c", "p", "co", 10)))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_Throw_Conflict_When_DuplicateExists()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var a = new Domain.Locations.Location("Arena", "s", "Budapest", "p", "Hungary", 100);
        var b = new Domain.Locations.Location("Dome", "s", "Budapest", "p", "Hungary", 100);
        db.Locations.AddRange(a, b);
        await db.SaveChangesAsync();

        var handler = new UpdateLocationCommandHandler(db);
        await FluentActions.Invoking(() => handler.HandleAsync(
                new UpdateLocationCommand(b.Id, "arena", "s", "budapest", "p", "hungary", 100)))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_Should_AllowIncreasingCapacity_WhenNoConstraintsViolated()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation(capacity: 100);
        db.Locations.Add(location);
        await db.SaveChangesAsync();

        var handler = new UpdateLocationCommandHandler(db);
        await handler.HandleAsync(new UpdateLocationCommand(
            location.Id, location.Name, location.Street, location.City, location.PostalCode, location.Country, 200));

        location.Capacity.Should().Be(200);
    }

    [Fact]
    public async Task Handle_Should_UpdateLocation()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation(capacity: 100);
        db.Locations.Add(location);
        await db.SaveChangesAsync();

        var handler = new UpdateLocationCommandHandler(db);
        await handler.HandleAsync(new UpdateLocationCommand(location.Id, "New", "s2", "City2", "p2", "Country2", 200));

        location.Name.Should().Be("New");
        location.Capacity.Should().Be(200);
    }
}
