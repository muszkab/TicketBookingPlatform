using Application.Locations.Queries.GetLocations;
using Application.Tests.Common;
using Domain.Locations;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Tests.Locations.Queries;

public class GetLocationsQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_ReturnAllLocations_OrderedByName()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        db.Locations.AddRange(
            new Location("Beta Hall", "s", "City", "p", "Co", 100),
            new Location("Alpha Arena", "s", "City", "p", "Co", 100),
            new Location("Gamma Dome", "s", "City", "p", "Co", 100));
        await db.SaveChangesAsync();

        var handler = new GetLocationsQueryHandler(db);
        var result = await handler.HandleAsync(new GetLocationsQuery());

        result.TotalCount.Should().Be(3);
        result.Items.Select(l => l.Name).Should().ContainInOrder("Alpha Arena", "Beta Hall", "Gamma Dome");
    }

    [Fact]
    public async Task Handle_Should_ApplyPaging()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        for (int i = 0; i < 5; i++)
            db.Locations.Add(new Location($"L{i}", "s", "C", "p", "Co", 50));
        await db.SaveChangesAsync();

        var handler = new GetLocationsQueryHandler(db);
        var result = await handler.HandleAsync(new GetLocationsQuery(Page: 2, PageSize: 2));

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.Page.Should().Be(2);
        result.TotalPages.Should().Be(3);
    }
}
