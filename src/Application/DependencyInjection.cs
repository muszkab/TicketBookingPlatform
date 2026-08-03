using Application.Locations.Queries.GetEventsByLocation;
using Application.Locations.Queries.GetLocationById;
using Application.Locations.Queries.GetLocations;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetLocationsQueryHandler>();
        services.AddScoped<GetLocationByIdQueryHandler>();
        services.AddScoped<GetEventsByLocationQueryHandler>();

        return services;
    }
}
