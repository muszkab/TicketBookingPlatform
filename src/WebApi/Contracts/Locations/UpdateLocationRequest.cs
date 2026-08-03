namespace WebApi.Contracts.Locations;

public sealed record UpdateLocationRequest(
    string Name,
    string Street,
    string City,
    string PostalCode,
    string Country,
    int Capacity);
