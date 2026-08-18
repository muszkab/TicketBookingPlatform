using Application.Events.Queries.GetEvents;
using Application.Tests.Common;
using Domain.Events;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Tests.Events.Queries;

public class GetEventsQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_FilterByCategoryAndStatus_AndOrderByStartsAt()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation();
        var concertLater = DomainFactory.NewOnSaleEventWithCategory(location.Id, out _);
        var concertEarlier = DomainFactory.NewOnSaleEventWithCategory(location.Id, out _);
        var theater = DomainFactory.NewDraftEvent(location.Id, "Theater");
        db.Locations.Add(location);
        db.Events.AddRange(concertLater, concertEarlier, theater);
        await db.SaveChangesAsync();

        var handler = new GetEventsQueryHandler(db);
        var result = await handler.HandleAsync(new GetEventsQuery(EventCategory.Concert, EventStatus.OnSale));

        result.Items.Should().HaveCount(2);
        result.Items.Select(e => e.StartsAt).Should().BeInAscendingOrder();
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Should_ApplyPaging()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation(capacity: 500);
        db.Locations.Add(location);
        for (int i = 0; i < 5; i++)
            db.Events.Add(DomainFactory.NewDraftEvent(location.Id, $"E{i}"));
        await db.SaveChangesAsync();

        var handler = new GetEventsQueryHandler(db);
        var result = await handler.HandleAsync(new GetEventsQuery(Page: 2, PageSize: 2));

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.Page.Should().Be(2);
    }
}
