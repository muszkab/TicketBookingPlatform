using Application.Auth.Commands.Login;
using Application.Auth.Commands.RegisterUser;
using Application.Events.Commands.AddTicketCategory;
using Application.Events.Commands.CancelEvent;
using Application.Events.Commands.CreateEvent;
using Application.Events.Commands.PublishEvent;
using Application.Events.Commands.RemoveTicketCategory;
using Application.Events.Commands.UpdateEvent;
using Application.Events.Queries.GetEventById;
using Application.Events.Queries.GetEvents;
using Application.Locations.Commands.CreateLocation;
using Application.Locations.Commands.DeleteLocation;
using Application.Locations.Commands.UpdateLocation;
using Application.Locations.Queries.GetEventsByLocation;
using Application.Locations.Queries.GetLocationById;
using Application.Locations.Queries.GetLocations;
using Application.Orders.Commands.CancelOrder;
using Application.Orders.Commands.CreateOrder;
using Application.Orders.Commands.PayOrder;
using Application.Orders.Queries.GetMyOrders;
using Application.Orders.Queries.GetOrderById;
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

        services.AddScoped<GetEventsQueryHandler>();
        services.AddScoped<GetEventByIdQueryHandler>();
        services.AddScoped<CreateEventCommandHandler>();
        services.AddScoped<UpdateEventCommandHandler>();
        services.AddScoped<AddTicketCategoryCommandHandler>();
        services.AddScoped<RemoveTicketCategoryCommandHandler>();
        services.AddScoped<PublishEventCommandHandler>();
        services.AddScoped<CancelEventCommandHandler>();

        services.AddScoped<RegisterUserCommandHandler>();
        services.AddScoped<LoginCommandHandler>();

        services.AddScoped<CreateOrderCommandHandler>();
        services.AddScoped<PayOrderCommandHandler>();
        services.AddScoped<CancelOrderCommandHandler>();
        services.AddScoped<GetOrderByIdQueryHandler>();
        services.AddScoped<GetMyOrdersQueryHandler>();

        return services;
    }
}
