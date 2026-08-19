using Application.Common.Interfaces;
using Application.Orders.Queries.GetOrderById;
using Application.Tests.Common;
using Domain.Orders;
using System;
using System.Threading.Tasks;

namespace Application.Tests.Orders.Queries;

public class GetOrderByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_Throw_When_NoCurrentUser()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns((Guid?)null);
        var handler = new GetOrderByIdQueryHandler(db, currentUser);

        await FluentActions.Invoking(() => handler.HandleAsync(new GetOrderByIdQuery(Guid.NewGuid())))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnNull_When_NotFound()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        var handler = new GetOrderByIdQueryHandler(db, currentUser);

        (await handler.HandleAsync(new GetOrderByIdQuery(Guid.NewGuid()))).Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnNull_When_OrderOwnedByAnotherUser()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var owner = Guid.NewGuid();
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out var category);
        var order = new Order(owner, ev.Id, "EUR");
        order.AddItem(category, 1);
        db.Locations.Add(location);
        db.Events.Add(ev);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        var handler = new GetOrderByIdQueryHandler(db, currentUser);

        (await handler.HandleAsync(new GetOrderByIdQuery(order.Id))).Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnDto_When_Owned()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var owner = Guid.NewGuid();
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out var category);
        var order = new Order(owner, ev.Id, "EUR");
        order.AddItem(category, 2);
        db.Locations.Add(location);
        db.Events.Add(ev);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner);
        var handler = new GetOrderByIdQueryHandler(db, currentUser);

        var dto = await handler.HandleAsync(new GetOrderByIdQuery(order.Id));

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(order.Id);
        dto.Items.Should().ContainSingle();
    }
}
