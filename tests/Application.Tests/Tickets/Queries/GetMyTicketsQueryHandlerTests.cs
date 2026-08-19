using Application.Common.Interfaces;
using Application.Tests.Common;
using Application.Tickets.Queries.GetMyTickets;
using Domain.Events;
using Domain.Orders;
using Domain.Tickets;
using Infrastructure.Persistence;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Tests.Tickets.Queries;

public class GetMyTicketsQueryHandlerTests
{
    private static async Task SeedTicketsAsync(ApplicationDbContext db, Guid userId, int quantity)
    {
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out var category, categoryQuantity: quantity + 5);
        var order = new Order(userId, ev.Id, "EUR");
        order.AddItem(category, quantity);
        var tickets = order.Pay(EventStatus.OnSale);

        db.Locations.Add(location);
        db.Events.Add(ev);
        db.Orders.Add(order);
        db.Tickets.AddRange(tickets);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_Should_Throw_When_NoCurrentUser()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns((Guid?)null);
        var handler = new GetMyTicketsQueryHandler(db, currentUser);

        await FluentActions.Invoking(() => handler.HandleAsync(new GetMyTicketsQuery()))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_Should_ReturnOnlyTicketsOfCurrentUser()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        var otherUser = Guid.NewGuid();
        await SeedTicketsAsync(db, userId, quantity: 2);
        await SeedTicketsAsync(db, otherUser, quantity: 3);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        var handler = new GetMyTicketsQueryHandler(db, currentUser);

        var result = await handler.HandleAsync(new GetMyTicketsQuery());

        result.TotalCount.Should().Be(2);
        result.Items.Select(t => t.CreatedAt).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Handle_Should_FilterByStatus()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var userId = Guid.NewGuid();
        await SeedTicketsAsync(db, userId, quantity: 2);

        var oneTicket = db.Tickets.First();
        oneTicket.Cancel();
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        var handler = new GetMyTicketsQueryHandler(db, currentUser);

        var validResult = await handler.HandleAsync(new GetMyTicketsQuery(Status: TicketStatus.Valid));
        var cancelledResult = await handler.HandleAsync(new GetMyTicketsQuery(Status: TicketStatus.Cancelled));

        validResult.TotalCount.Should().Be(1);
        cancelledResult.TotalCount.Should().Be(1);
        cancelledResult.Items[0].Id.Should().Be(oneTicket.Id);
    }
}
