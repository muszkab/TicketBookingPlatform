using Domain.Common;
using Domain.Common.Exceptions;
using Domain.Events;
using System;

namespace Domain.Tests.Events;

public class TicketCategoryTests
{
    private static Money Price(decimal amount = 10m) => new(amount, "EUR");

    [Fact]
    public void Ctor_Should_Throw_When_EventIdEmpty()
    {
        Action act = () => new TicketCategory(Guid.Empty, "Std", Price(), 10);
        act.Should().Throw<ArgumentException>().WithParameterName("eventId");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Ctor_Should_Throw_When_NameMissing(string name)
    {
        Action act = () => new TicketCategory(Guid.NewGuid(), name, Price(), 10);
        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Ctor_Should_Throw_When_PriceNull()
    {
        Action act = () => new TicketCategory(Guid.NewGuid(), "Std", null!, 10);
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Ctor_Should_Throw_When_TotalQuantityNotPositive(int qty)
    {
        Action act = () => new TicketCategory(Guid.NewGuid(), "Std", Price(), qty);
        act.Should().Throw<ArgumentException>().WithParameterName("totalQuantity");
    }

    [Fact]
    public void Ctor_Should_InitializeAvailableToTotal()
    {
        var cat = new TicketCategory(Guid.NewGuid(), "Std", Price(), 10);
        cat.AvailableQuantity.Should().Be(10);
        cat.TotalQuantity.Should().Be(10);
    }

    [Fact]
    public void Reserve_Should_DecreaseAvailable()
    {
        var cat = new TicketCategory(Guid.NewGuid(), "Std", Price(), 10);
        cat.Reserve(3);
        cat.AvailableQuantity.Should().Be(7);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reserve_Should_Throw_When_QuantityNotPositive(int qty)
    {
        var cat = new TicketCategory(Guid.NewGuid(), "Std", Price(), 10);
        Action act = () => cat.Reserve(qty);
        act.Should().Throw<ArgumentException>().WithParameterName("quantity");
    }

    [Fact]
    public void Reserve_Should_Throw_When_NotEnoughAvailable()
    {
        var cat = new TicketCategory(Guid.NewGuid(), "Std", Price(), 2);
        Action act = () => cat.Reserve(3);
        act.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Release_Should_IncreaseAvailable()
    {
        var cat = new TicketCategory(Guid.NewGuid(), "Std", Price(), 10);
        cat.Reserve(4);
        cat.Release(2);
        cat.AvailableQuantity.Should().Be(8);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Release_Should_Throw_When_QuantityNotPositive(int qty)
    {
        var cat = new TicketCategory(Guid.NewGuid(), "Std", Price(), 10);
        Action act = () => cat.Release(qty);
        act.Should().Throw<ArgumentException>().WithParameterName("quantity");
    }

    [Fact]
    public void Release_Should_Throw_When_ExceedsTotal()
    {
        var cat = new TicketCategory(Guid.NewGuid(), "Std", Price(), 10);
        Action act = () => cat.Release(1);
        act.Should().Throw<BusinessRuleException>();
    }
}
