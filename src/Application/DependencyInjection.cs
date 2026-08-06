using Application.Events.Commands.AddTicketCategory;
using Application.Events.Commands.PublishEvent;
using Application.Events.Commands.CancelEvent;
using Application.Events.Commands.CreateEvent;
using Application.Events.Queries.GetEventById;
using Application.Events.Queries.GetEvents;
using Application.Locations.Commands.CreateLocation;
using Application.Locations.Commands.DeleteLocation;
using Application.Locations.Commands.UpdateLocation;
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
        services.AddScoped<CreateLocationCommandHandler>();
        services.AddScoped<UpdateLocationCommandHandler>();
        services.AddScoped<DeleteLocationCommandHandler>();

        services.AddScoped<GetEventByIdQueryHandler>();
        services.AddScoped<GetEventsQueryHandler>();
        services.AddScoped<CreateEventCommandHandler>();
        services.AddScoped<AddTicketCategoryCommandHandler>();
        services.AddScoped<PublishEventCommandHandler>();
        services.AddScoped<CancelEventCommandHandler>();

        return services;
    }
}
