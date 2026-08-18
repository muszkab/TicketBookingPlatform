using Domain.Common;
using Domain.Events;
using Domain.Orders;
using System;

namespace Domain.Tests.Orders;

public class OrderItemTests
{
    private static TicketCategory NewCategory()
        => new(Guid.NewGuid(), "Std", new Money(10m, "EUR"), 100);

    [Fact]
    public void Ctor_Should_Throw_When_OrderIdEmpty()
    {
        Action act = () => new OrderItem(Guid.Empty, NewCategory(), 1, new Money(10m, "EUR"));
        act.Should().Throw<ArgumentException>().WithParameterName("orderId");
    }

    [Fact]
    public void Ctor_Should_Throw_When_CategoryNull()
    {
        Action act = () => new OrderItem(Guid.NewGuid(), null!, 1, new Money(10m, "EUR"));
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Ctor_Should_Throw_When_QuantityNotPositive(int qty)
    {
        Action act = () => new OrderItem(Guid.NewGuid(), NewCategory(), qty, new Money(10m, "EUR"));
        act.Should().Throw<ArgumentException>().WithParameterName("quantity");
    }

    [Fact]
    public void Ctor_Should_Throw_When_UnitPriceNull()
    {
        Action act = () => new OrderItem(Guid.NewGuid(), NewCategory(), 1, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void LineTotal_Should_BeUnitPriceTimesQuantity()
    {
        var item = new OrderItem(Guid.NewGuid(), NewCategory(), 3, new Money(10m, "EUR"));
        item.LineTotal.Should().Be(new Money(30m, "EUR"));
    }
}
