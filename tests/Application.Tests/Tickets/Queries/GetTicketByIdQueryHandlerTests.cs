using Application.Common.Interfaces;
using Application.Tests.Common;
using Application.Tickets.Queries.GetTicketById;
using Domain.Events;
using Domain.Orders;
using Infrastructure.Persistence;
using System;
using System.Threading.Tasks;

namespace Application.Tests.Tickets.Queries;

public class GetTicketByIdQueryHandlerTests
{
    private static async Task<(Order order, Guid ticketId)> SeedPaidOrderAsync(ApplicationDbContext db, Guid userId)
    {
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out var category);
        var order = new Order(userId, ev.Id, "EUR");
        order.AddItem(category, 1);
        var tickets = order.Pay(EventStatus.OnSale);

        db.Locations.Add(location);
        db.Events.Add(ev);
        db.Orders.Add(order);
        db.Tickets.AddRange(tickets);
        await db.SaveChangesAsync();
        return (order, tickets[0].Id);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_NoCurrentUser()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns((Guid?)null);
        var handler = new GetTicketByIdQueryHandler(db, currentUser);

        await FluentActions.Invoking(() => handler.HandleAsync(new GetTicketByIdQuery(Guid.NewGuid())))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnNull_When_NotFound()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        var handler = new GetTicketByIdQueryHandler(db, currentUser);

        (await handler.HandleAsync(new GetTicketByIdQuery(Guid.NewGuid()))).Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnNull_When_TicketOwnedByAnotherUser()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var owner = Guid.NewGuid();
        var (_, ticketId) = await SeedPaidOrderAsync(db, owner);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        var handler = new GetTicketByIdQueryHandler(db, currentUser);

        (await handler.HandleAsync(new GetTicketByIdQuery(ticketId))).Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ReturnDto_When_Owned()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var owner = Guid.NewGuid();
        var (order, ticketId) = await SeedPaidOrderAsync(db, owner);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(owner);
        var handler = new GetTicketByIdQueryHandler(db, currentUser);

        var dto = await handler.HandleAsync(new GetTicketByIdQuery(ticketId));

        dto.Should().NotBeNull();
        dto!.Id.Should().Be(ticketId);
        dto.OrderId.Should().Be(order.Id);
        dto.Code.Should().NotBeNullOrEmpty();
    }
}
