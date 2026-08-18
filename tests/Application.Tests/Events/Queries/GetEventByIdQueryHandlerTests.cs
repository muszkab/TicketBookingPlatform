using Application.Events.Queries.GetEventById;
using Application.Tests.Common;
using System;
using System.Threading.Tasks;

namespace Application.Tests.Events.Queries;

public class GetEventByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnNull_When_NotFound()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var handler = new GetEventByIdQueryHandler(db);
        var result = await handler.HandleAsync(new GetEventByIdQuery(Guid.NewGuid()));
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnDto_When_Found()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out _);
        db.Locations.Add(location);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var handler = new GetEventByIdQueryHandler(db);
        var dto = await handler.HandleAsync(new GetEventByIdQuery(ev.Id));

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(ev.Id);
        dto.TicketCategories.Should().ContainSingle();
    }
}
