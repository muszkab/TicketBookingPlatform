using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Orders.Commands.PayOrder;
using Application.Tests.Common;
using Domain.Events;
using Domain.Orders;
using Infrastructure.Persistence;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Tests.Orders.Commands;

public class PayOrderCommandHandlerTests
{
    private static (PayOrderCommandHandler handler, ApplicationDbContext db) BuildSut(Guid? userId)
    {
        var db = TestDbContextFactory.CreateInMemory();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        return (new PayOrderCommandHandler(db, currentUser), db);
    }

    private static async Task<(Order order, Event ev)> SeedPendingOrderAsync(ApplicationDbContext db, Guid userId, int quantity = 2)
    {
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out var category);
        var order = new Order(userId, ev.Id, "EUR");
        order.AddItem(category, quantity);

        db.Locations.Add(location);
        db.Events.Add(ev);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return (order, ev);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_NoCurrentUser()
    {
        var (handler, _) = BuildSut(userId: null);
        await FluentActions.Invoking(() => handler.HandleAsync(new PayOrderCommand(Guid.NewGuid())))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_OrderMissing()
    {
        var (handler, _) = BuildSut(Guid.NewGuid());
        await FluentActions.Invoking(() => handler.HandleAsync(new PayOrderCommand(Guid.NewGuid())))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_OrderOwnedByAnotherUser()
    {
        var owner = Guid.NewGuid();
        var (handler, db) = BuildSut(userId: Guid.NewGuid());
        var (order, _) = await SeedPendingOrderAsync(db, owner);

        await FluentActions.Invoking(() => handler.HandleAsync(new PayOrderCommand(order.Id)))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_MarkPaid_And_IssueTickets()
    {
        var userId = Guid.NewGuid();
        var (handler, db) = BuildSut(userId);
        var (order, _) = await SeedPendingOrderAsync(db, userId, quantity: 3);

        var dto = await handler.HandleAsync(new PayOrderCommand(order.Id));

        dto.Status.Should().Be(OrderStatus.Paid);
        dto.PaidAt.Should().NotBeNull();

        db.Tickets.Count().Should().Be(3);
        db.Tickets.Select(t => t.OrderId).Should().OnlyContain(id => id == order.Id);
    }

    [Fact]
    public async Task Handle_Should_MarkEventSoldOut_When_LastTicketsPaid()
    {
        var userId = Guid.NewGuid();
        var (handler, db) = BuildSut(userId);

        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out var category, categoryQuantity: 3);
        var order = new Order(userId, ev.Id, "EUR");
        order.AddItem(category, 3);

        db.Locations.Add(location);
        db.Events.Add(ev);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await handler.HandleAsync(new PayOrderCommand(order.Id));

        ev.Status.Should().Be(EventStatus.SoldOut);
        category.AvailableQuantity.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Should_KeepEventOnSale_When_TicketsRemain()
    {
        var userId = Guid.NewGuid();
        var (handler, db) = BuildSut(userId);
        var (order, ev) = await SeedPendingOrderAsync(db, userId, quantity: 2);

        await handler.HandleAsync(new PayOrderCommand(order.Id));

        ev.Status.Should().Be(EventStatus.OnSale);
    }
}
