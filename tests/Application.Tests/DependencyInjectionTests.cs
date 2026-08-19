using Application.Auth.Commands.Login;
using Application.Auth.Commands.RegisterUser;
using Application.Common.Interfaces;
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
using Application.Tests.Common;
using Application.Tickets.Queries.GetMyTickets;
using Application.Tickets.Queries.GetTicketById;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace Application.Tests;

public class DependencyInjectionTests
{
    private static readonly Type[] ExpectedHandlers =
    [
        typeof(GetLocationsQueryHandler),
        typeof(GetLocationByIdQueryHandler),
        typeof(GetEventsByLocationQueryHandler),
        typeof(CreateLocationCommandHandler),
        typeof(UpdateLocationCommandHandler),
        typeof(DeleteLocationCommandHandler),
        typeof(GetEventsQueryHandler),
        typeof(GetEventByIdQueryHandler),
        typeof(CreateEventCommandHandler),
        typeof(UpdateEventCommandHandler),
        typeof(AddTicketCategoryCommandHandler),
        typeof(RemoveTicketCategoryCommandHandler),
        typeof(PublishEventCommandHandler),
        typeof(CancelEventCommandHandler),
        typeof(RegisterUserCommandHandler),
        typeof(LoginCommandHandler),
        typeof(CreateOrderCommandHandler),
        typeof(PayOrderCommandHandler),
        typeof(CancelOrderCommandHandler),
        typeof(GetOrderByIdQueryHandler),
        typeof(GetMyOrdersQueryHandler),
        typeof(GetTicketByIdQueryHandler),
        typeof(GetMyTicketsQueryHandler),
    ];

    [Fact]
    public void AddApplication_Should_ResolveAllHandlers()
    {
        var services = new ServiceCollection();

        services.AddScoped<IApplicationDbContext>(_ => TestDbContextFactory.CreateInMemory());
        services.AddScoped(_ => Substitute.For<ICurrentUserService>());
        services.AddScoped(_ => Substitute.For<IPasswordHasher>());
        services.AddScoped(_ => Substitute.For<IJwtTokenGenerator>());

        services.AddApplication();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        foreach (var handlerType in ExpectedHandlers)
        {
            var handler = scope.ServiceProvider.GetService(handlerType);
            handler.Should().NotBeNull($"handler '{handlerType.Name}' should be registered by AddApplication");
        }
    }

    [Fact]
    public void AddApplication_Should_CoverAllHandlerTypesInAssembly()
    {
        var handlerTypesInAssembly = typeof(DependencyInjection).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        (t.Name.EndsWith("CommandHandler") || t.Name.EndsWith("QueryHandler")))
            .ToArray();

        handlerTypesInAssembly.Should().BeEquivalentTo(ExpectedHandlers,
            "the DI smoke test list should be kept in sync with the actual handlers in the Application assembly");
    }
}
