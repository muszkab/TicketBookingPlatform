using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Orders.Commands.CancelOrder;
using Application.Tests.Common;
using Domain.Orders;
using Infrastructure.Persistence;
using System;
using System.Threading.Tasks;

namespace Application.Tests.Orders.Commands;

public class CancelOrderCommandHandlerTests
{
    private static (CancelOrderCommandHandler handler, ApplicationDbContext db) BuildSut(Guid? userId)
    {
        var db = TestDbContextFactory.CreateInMemory();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        return (new CancelOrderCommandHandler(db, currentUser), db);
    }

    private static async Task<Order> SeedPendingOrderAsync(ApplicationDbContext db, Guid userId)
    {
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out var category);
        var order = new Order(userId, ev.Id, "EUR");
        order.AddItem(category, 1);

        db.Locations.Add(location);
        db.Events.Add(ev);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    [Fact]
    public async Task Handle_Should_Throw_When_NoCurrentUser()
    {
        var (handler, _) = BuildSut(userId: null);
        await FluentActions.Invoking(() => handler.HandleAsync(new CancelOrderCommand(Guid.NewGuid())))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_OrderMissing()
    {
        var (handler, _) = BuildSut(Guid.NewGuid());
        await FluentActions.Invoking(() => handler.HandleAsync(new CancelOrderCommand(Guid.NewGuid())))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_OrderOwnedByAnotherUser()
    {
        var owner = Guid.NewGuid();
        var (handler, db) = BuildSut(userId: Guid.NewGuid());
        var order = await SeedPendingOrderAsync(db, owner);

        await FluentActions.Invoking(() => handler.HandleAsync(new CancelOrderCommand(order.Id)))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_CancelOrder()
    {
        var userId = Guid.NewGuid();
        var (handler, db) = BuildSut(userId);
        var order = await SeedPendingOrderAsync(db, userId);

        var dto = await handler.HandleAsync(new CancelOrderCommand(order.Id));

        dto.Status.Should().Be(OrderStatus.Cancelled);
        dto.CancelledAt.Should().NotBeNull();
    }
}
