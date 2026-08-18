using Domain.Locations;
using System;

namespace Domain.Tests.Locations;

public class LocationTests
{
    [Theory]
    [InlineData("", "City", "Country")]
    [InlineData("Name", "", "Country")]
    [InlineData("Name", "City", "")]
    public void Ctor_Should_Throw_When_RequiredFieldsMissing(string name, string city, string country)
    {
        Action act = () => new Location(name, "street", city, "1234", country, 100);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Ctor_Should_Throw_When_CapacityNotPositive(int capacity)
    {
        Action act = () => new Location("N", "s", "c", "p", "co", capacity);
        act.Should().Throw<ArgumentException>().WithParameterName("capacity");
    }

    [Fact]
    public void Ctor_Should_ReplaceNullOptionalFields_WithEmpty()
    {
        var loc = new Location("N", null!, "c", null!, "co", 10);
        loc.Street.Should().BeEmpty();
        loc.PostalCode.Should().BeEmpty();
    }

    [Fact]
    public void UpdateDetails_Should_UpdateAllFields()
    {
        var loc = new Location("N", "s", "c", "p", "co", 10);
        loc.UpdateDetails("N2", "s2", "c2", "p2", "co2", 20);

        loc.Name.Should().Be("N2");
        loc.Street.Should().Be("s2");
        loc.City.Should().Be("c2");
        loc.PostalCode.Should().Be("p2");
        loc.Country.Should().Be("co2");
        loc.Capacity.Should().Be(20);
    }

    [Fact]
    public void UpdateDetails_Should_Validate()
    {
        var loc = new Location("N", "s", "c", "p", "co", 10);
        Action act = () => loc.UpdateDetails("", "s", "c", "p", "co", 10);
        act.Should().Throw<ArgumentException>();
    }
}
