using System;

namespace Application.Locations.Commands.UpdateLocation;

public sealed record UpdateLocationCommand(
    Guid Id,
    string Name,
    string Street,
    string City,
    string PostalCode,
    string Country,
    int Capacity);
