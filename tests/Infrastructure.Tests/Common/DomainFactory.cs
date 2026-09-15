using Domain.Common;
using Domain.Events;
using Domain.Locations;
using Domain.Orders;
using Domain.Users;
using System;

namespace Infrastructure.Tests.Common;

internal static class DomainFactory
{
    public static User NewUser(
        string email = "user@example.com",
        string passwordHash = "hash",
        string fullName = "Test User",
        UserRole role = UserRole.Customer)
        => new(email, passwordHash, fullName, role);

    public static Location NewLocation(
        string name = "Arena",
        string city = "City",
        string country = "Country",
        int capacity = 100)
        => new(name, "Street 1", city, "1234", country, capacity);

    public static Event NewDraftEvent(Guid locationId, string title = "Concert")
        => new(
            title,
            "desc",
            EventCategory.Concert,
            DateTimeOffset.UtcNow.AddDays(10),
            DateTimeOffset.UtcNow.AddDays(10).AddHours(3),
            locationId);

    public static Event NewOnSaleEventWithCategory(
        Guid locationId,
        out TicketCategory category,
        int categoryQuantity = 20,
        int locationCapacity = 100,
        decimal price = 10m,
        string categoryName = "Std")
    {
        Event ev = NewDraftEvent(locationId);
        category = ev.AddTicketCategory(categoryName, new Money(price, "EUR"), categoryQuantity, locationCapacity);
        ev.PutOnSale();
        return ev;
    }

    public static Order NewPendingOrder(Guid userId, Event ev, TicketCategory category, int quantity = 2)
    {
        var order = new Order(userId, ev.Id, category.Price.Currency);
        order.AddItem(category, quantity);
        return order;
    }
}
