using System;

namespace Application.Locations.Queries.GetLocations;

public sealed record LocationDto(
    Guid Id,
    string Name,
    string Street,
    string City,
    string PostalCode,
    string Country,
    int Capacity);
