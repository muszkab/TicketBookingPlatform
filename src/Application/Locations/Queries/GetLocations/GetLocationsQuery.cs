namespace Application.Locations.Queries.GetLocations;

public sealed record GetLocationsQuery(
    string? Name = null,
    string? City = null,
    int Page = 1,
    int PageSize = GetLocationsQuery.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
}
