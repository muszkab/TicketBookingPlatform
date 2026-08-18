using Application.Common.Exceptions;
using Application.Events.Commands.CancelEvent;
using Application.Tests.Common;
using Domain.Events;
using Domain.Orders;
using Domain.Tickets;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Application.Tests.Events.Commands;

public class CancelEventCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_EventMissing()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var handler = new CancelEventCommandHandler(db);
        await FluentActions.Invoking(() => handler.HandleAsync(new CancelEventCommand(Guid.NewGuid())))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_CancelEvent_And_PendingOrders_And_ValidTickets()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out var category);
        var pendingOrder = new Order(Guid.NewGuid(), ev.Id, "EUR");
        pendingOrder.AddItem(category, 1);
        var ticket = new Ticket(Guid.NewGuid(), Guid.NewGuid(), ev.Id, category.Id);

        db.Locations.Add(location);
        db.Events.Add(ev);
        db.Orders.Add(pendingOrder);
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();

        var handler = new CancelEventCommandHandler(db);
        await handler.HandleAsync(new CancelEventCommand(ev.Id));

        ev.Status.Should().Be(EventStatus.Cancelled);
        db.Orders.Single(o => o.Id == pendingOrder.Id).Status.Should().Be(OrderStatus.Cancelled);
        db.Tickets.Single(t => t.Id == ticket.Id).Status.Should().Be(TicketStatus.Cancelled);
    }
}
