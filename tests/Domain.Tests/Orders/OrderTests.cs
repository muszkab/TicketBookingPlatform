using Domain.Common;
using Domain.Common.Exceptions;
using Domain.Events;
using Domain.Orders;
using Domain.Tests.Common.Builders;
using System;
using System.Linq;

namespace Domain.Tests.Orders;

public class OrderTests
{
    [Fact]
    public void Ctor_Should_Throw_When_UserIdEmpty()
    {
        Action act = () => new Order(Guid.Empty, Guid.NewGuid(), "EUR");
        act.Should().Throw<ArgumentException>().WithParameterName("userId");
    }

    [Fact]
    public void Ctor_Should_Throw_When_EventIdEmpty()
    {
        Action act = () => new Order(Guid.NewGuid(), Guid.Empty, "EUR");
        act.Should().Throw<ArgumentException>().WithParameterName("eventId");
    }

    [Fact]
    public void Ctor_Should_InitializePendingWithZeroTotal()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "EUR");
        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Should().Be(Money.Zero("EUR"));
        order.Items.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_Should_Throw_When_NotPending()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "EUR");
        order.Cancel();
        var category = OrderBuilder.NewCategory(order.EventId);

        Action act = () => order.AddItem(category, 1);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void AddItem_Should_Throw_When_CategoryNull()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "EUR");
        Action act = () => order.AddItem(null!, 1);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddItem_Should_Throw_When_CategoryFromDifferentEvent()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "EUR");
        var otherCategory = OrderBuilder.NewCategory(Guid.NewGuid());
        Action act = () => order.AddItem(otherCategory, 1);
        act.Should().Throw<BusinessRuleException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void AddItem_Should_Throw_When_QuantityNotPositive(int qty)
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "EUR");
        var category = OrderBuilder.NewCategory(order.EventId);
        Action act = () => order.AddItem(category, qty);
        act.Should().Throw<ArgumentException>().WithParameterName("quantity");
    }

    [Fact]
    public void AddItem_Should_Throw_When_DuplicateCategory()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "EUR");
        var category = OrderBuilder.NewCategory(order.EventId);
        order.AddItem(category, 1);
        Action act = () => order.AddItem(category, 2);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void AddItem_Should_AppendItem_And_UpdateTotal()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "EUR");
        var catA = OrderBuilder.NewCategory(order.EventId, "A", price: 10m);
        var catB = OrderBuilder.NewCategory(order.EventId, "B", price: 15m);

        order.AddItem(catA, 2);
        order.AddItem(catB, 3);

        order.Items.Should().HaveCount(2);
        order.TotalAmount.Should().Be(new Money(2 * 10m + 3 * 15m, "EUR"));
    }

    [Fact]
    public void Pay_Should_Throw_When_NotPending()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "EUR");
        order.Cancel();
        Action act = () => order.Pay(EventStatus.OnSale);
        act.Should().Throw<BusinessRuleException>();
    }

    [Theory]
    [InlineData(EventStatus.Draft)]
    [InlineData(EventStatus.SoldOut)]
    [InlineData(EventStatus.Cancelled)]
    [InlineData(EventStatus.Completed)]
    public void Pay_Should_Throw_When_EventNotOnSale(EventStatus status)
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "EUR");
        var category = OrderBuilder.NewCategory(order.EventId);
        order.AddItem(category, 1);

        Action act = () => order.Pay(status);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Pay_Should_Throw_When_NoItems()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "EUR");
        Action act = () => order.Pay(EventStatus.OnSale);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Pay_Should_ReserveCategories_IssueTickets_And_MarkPaid()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "EUR");
        var catA = OrderBuilder.NewCategory(order.EventId, "A", quantity: 10);
        var catB = OrderBuilder.NewCategory(order.EventId, "B", quantity: 10);
        order.AddItem(catA, 2);
        order.AddItem(catB, 3);

        var tickets = order.Pay(EventStatus.OnSale);

        tickets.Should().HaveCount(5);
        catA.AvailableQuantity.Should().Be(8);
        catB.AvailableQuantity.Should().Be(7);
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAt.Should().NotBeNull();
        tickets.Select(t => t.OrderId).Should().OnlyContain(id => id == order.Id);
        tickets.Where(t => t.TicketCategoryId == catA.Id).Should().HaveCount(2);
        tickets.Where(t => t.TicketCategoryId == catB.Id).Should().HaveCount(3);
    }

    [Fact]
    public void Cancel_Should_Throw_When_NotPending()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "EUR");
        var category = OrderBuilder.NewCategory(order.EventId, quantity: 5);
        order.AddItem(category, 1);
        order.Pay(EventStatus.OnSale);

        Action act = () => order.Cancel();
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Cancel_Should_SetCancelled_And_Timestamp()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), "EUR");
        order.Cancel();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelledAt.Should().NotBeNull();
    }
}
