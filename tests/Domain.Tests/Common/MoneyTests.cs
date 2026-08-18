using Domain.Common;
using System;

namespace Domain.Tests.Common;

public class MoneyTests
{
    [Fact]
    public void Ctor_Should_Throw_When_AmountIsNegative()
    {
        Action act = () => new Money(-1m, "EUR");
        act.Should().Throw<ArgumentException>().WithParameterName("amount");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Ctor_Should_Throw_When_CurrencyIsMissing(string? currency)
    {
        Action act = () => new Money(1m, currency!);
        act.Should().Throw<ArgumentException>().WithParameterName("currency");
    }

    [Theory]
    [InlineData("EU")]
    [InlineData("EURO")]
    public void Ctor_Should_Throw_When_CurrencyIsNotThreeLetters(string currency)
    {
        Action act = () => new Money(1m, currency);
        act.Should().Throw<ArgumentException>().WithParameterName("currency");
    }

    [Fact]
    public void Ctor_Should_UpperCaseCurrency()
    {
        var money = new Money(10m, "eur");
        money.Currency.Should().Be("EUR");
        money.Amount.Should().Be(10m);
    }

    [Fact]
    public void Zero_Should_ReturnZeroAmount()
    {
        var money = Money.Zero("USD");
        money.Amount.Should().Be(0m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_Should_Throw_When_CurrencyDiffers()
    {
        var a = new Money(1m, "EUR");
        var b = new Money(1m, "USD");
        Action act = () => a.Add(b);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Add_Should_SumAmounts_When_SameCurrency()
    {
        var result = new Money(10m, "EUR").Add(new Money(5m, "EUR"));
        result.Should().Be(new Money(15m, "EUR"));
    }

    [Fact]
    public void Multiply_Should_Throw_When_QuantityIsNegative()
    {
        Action act = () => new Money(10m, "EUR").Multiply(-1);
        act.Should().Throw<ArgumentException>().WithParameterName("quantity");
    }

    [Fact]
    public void Multiply_Should_ReturnZero_When_QuantityIsZero()
    {
        new Money(10m, "EUR").Multiply(0).Should().Be(new Money(0m, "EUR"));
    }

    [Fact]
    public void Multiply_Should_ReturnProduct()
    {
        new Money(10m, "EUR").Multiply(3).Should().Be(new Money(30m, "EUR"));
    }

    [Fact]
    public void Equals_Should_BeTrue_When_AmountAndCurrencyMatch()
    {
        new Money(10m, "EUR").Should().Be(new Money(10m, "EUR"));
        new Money(10m, "EUR").GetHashCode().Should().Be(new Money(10m, "EUR").GetHashCode());
    }

    [Fact]
    public void Equals_Should_BeFalse_When_AmountOrCurrencyDiffers()
    {
        new Money(10m, "EUR").Should().NotBe(new Money(11m, "EUR"));
        new Money(10m, "EUR").Should().NotBe(new Money(10m, "USD"));
    }
}
