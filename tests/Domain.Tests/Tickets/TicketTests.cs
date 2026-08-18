using Domain.Tickets;
using System;

namespace Domain.Tests.Tickets;

public class TicketTests
{
    [Fact]
    public void Ctor_Should_Throw_When_OrderIdEmpty()
    {
        Action act = () => new Ticket(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithParameterName("orderId");
    }

    [Fact]
    public void Ctor_Should_Throw_When_OrderItemIdEmpty()
    {
        Action act = () => new Ticket(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithParameterName("orderItemId");
    }

    [Fact]
    public void Ctor_Should_Throw_When_EventIdEmpty()
    {
        Action act = () => new Ticket(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid());
        act.Should().Throw<ArgumentException>().WithParameterName("eventId");
    }

    [Fact]
    public void Ctor_Should_Throw_When_TicketCategoryIdEmpty()
    {
        Action act = () => new Ticket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);
        act.Should().Throw<ArgumentException>().WithParameterName("ticketCategoryId");
    }

    [Fact]
    public void Ctor_Should_InitializeAsValid_WithCode()
    {
        var ticket = new Ticket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        ticket.Status.Should().Be(TicketStatus.Valid);
        ticket.Code.Should().NotBeNullOrEmpty();
        ticket.UsedAt.Should().BeNull();
        ticket.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsUsed_Should_SetUsedAt_And_Status()
    {
        var ticket = new Ticket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        ticket.MarkAsUsed();
        ticket.Status.Should().Be(TicketStatus.Used);
        ticket.UsedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsUsed_Should_Throw_When_NotValid()
    {
        var ticket = new Ticket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        ticket.Cancel();
        Action act = () => ticket.MarkAsUsed();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_Should_Throw_When_Used()
    {
        var ticket = new Ticket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        ticket.MarkAsUsed();
        Action act = () => ticket.Cancel();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_Should_SetCancelled_When_Valid()
    {
        var ticket = new Ticket(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        ticket.Cancel();
        ticket.Status.Should().Be(TicketStatus.Cancelled);
    }
}
