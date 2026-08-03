using System;
using Domain.Common;

namespace Domain.Locations;

public class Location : Entity
{
    public string Name { get; private set; }
    public string Street { get; private set; }
    public string City { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }
    public int Capacity { get; private set; }

    private Location()
    {
        Name = string.Empty;
        Street = string.Empty;
        City = string.Empty;
        PostalCode = string.Empty;
        Country = string.Empty;
    }

    public Location(
        string name,
        string street,
        string city,
        string postalCode,
        string country,
        int capacity)
    {
        Validate(name, city, country, capacity);

        Name = name;
        Street = street ?? string.Empty;
        City = city;
        PostalCode = postalCode ?? string.Empty;
        Country = country;
        Capacity = capacity;
    }

    public void UpdateDetails(
        string name,
        string street,
        string city,
        string postalCode,
        string country,
        int capacity)
    {
        Validate(name, city, country, capacity);

        Name = name;
        City = city;
        Country = country;
        Street = street ?? string.Empty;
        PostalCode = postalCode ?? string.Empty;
        Capacity = capacity;
    }

    private static void Validate(string name, string city, string country, int capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City is required.", nameof(city));

        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Country is required.", nameof(country));

        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than zero.", nameof(capacity));
    }
}
