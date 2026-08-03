namespace Application.Locations.Commands.CreateLocation;

public sealed record CreateLocationCommand(
    string Name,
    string Street,
    string City,
    string PostalCode,
    string Country,
    int Capacity);
