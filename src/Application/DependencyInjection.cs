using Application.Locations.Queries.GetLocations;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GetLocationsQueryHandler>();

        return services;
    }
}
