using Application.Common.Exceptions;
using Application.Locations.Commands.CreateLocation;
using Application.Tests.Common;
using System.Threading.Tasks;

namespace Application.Tests.Locations.Commands;

public class CreateLocationCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Throw_Conflict_When_DuplicateNameCityCountry()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        db.Locations.Add(new Domain.Locations.Location("Arena", "s1", "Budapest", "1000", "Hungary", 100));
        await db.SaveChangesAsync();

        var handler = new CreateLocationCommandHandler(db);
        var command = new CreateLocationCommand("arena", "s2", "BUDAPEST", "2000", "hungary", 50);

        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_Should_CreateLocation()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var handler = new CreateLocationCommandHandler(db);
        var command = new CreateLocationCommand("Arena", "s", "Budapest", "1000", "Hungary", 500);

        var dto = await handler.HandleAsync(command);

        dto.Name.Should().Be("Arena");
        dto.Capacity.Should().Be(500);
        db.Locations.Should().HaveCount(1);
    }
}
