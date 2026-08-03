namespace Application.Locations.Queries.SearchLocations;

public sealed record SearchLocationsQuery(
    string? Name,
    string? City,
    int Page = 1,
    int PageSize = SearchLocationsQuery.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
}
