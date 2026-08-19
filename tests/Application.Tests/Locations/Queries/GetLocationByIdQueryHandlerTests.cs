using Application.Locations.Queries.GetLocationById;
using Application.Tests.Common;
using System;
using System.Threading.Tasks;

namespace Application.Tests.Locations.Queries;

public class GetLocationByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnNull_When_NotFound()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var handler = new GetLocationByIdQueryHandler(db);
        var result = await handler.HandleAsync(new GetLocationByIdQuery(Guid.NewGuid()));
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnDto_When_Found()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation(capacity: 300);
        db.Locations.Add(location);
        await db.SaveChangesAsync();

        var handler = new GetLocationByIdQueryHandler(db);
        var dto = await handler.HandleAsync(new GetLocationByIdQuery(location.Id));

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(location.Id);
        dto.Capacity.Should().Be(300);
    }
}
