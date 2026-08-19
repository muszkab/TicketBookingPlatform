using Application.Common.Interfaces;
using Application.Orders.Queries.GetMyOrders;
using Application.Tests.Common;
using Domain.Orders;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Tests.Orders.Queries;

public class GetMyOrdersQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_Throw_When_NoCurrentUser()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns((Guid?)null);
        var handler = new GetMyOrdersQueryHandler(db, currentUser);

        await FluentActions.Invoking(() => handler.HandleAsync(new GetMyOrdersQuery()))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnOnlyOrdersOfCurrentUser_OrderedByCreatedDesc()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out var category);

        var o1 = new Order(userId, ev.Id, "EUR"); o1.AddItem(category, 1);
        var o2 = new Order(userId, ev.Id, "EUR"); o2.AddItem(category, 2);
        var o3 = new Order(otherUser, ev.Id, "EUR"); o3.AddItem(category, 1);

        db.Locations.Add(location);
        db.Events.Add(ev);
        db.Orders.AddRange(o1, o2, o3);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        var handler = new GetMyOrdersQueryHandler(db, currentUser);

        var result = await handler.HandleAsync(new GetMyOrdersQuery());

        result.Items.Should().HaveCount(2);
        result.Items.Select(i => i.CreatedAt).Should().BeInDescendingOrder();
    }
}
