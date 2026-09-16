using Domain.Common;
using Domain.Events;
using Domain.Locations;
using Domain.Users;
using System;

namespace WebApi.Tests.Integration.Common;

internal static class TestDataSeeder
{
    public static User NewUser(
        string email = "customer@example.com",
        string fullName = "Test Customer",
        UserRole role = UserRole.Customer)
        => new(email, "hash", fullName, role);

    public static User NewAdmin(string email = "admin@example.com")
        => NewUser(email, "Test Admin", UserRole.Admin);

    public static Location NewLocation(string name = "Test Arena", int capacity = 500)
        => new(name, "Street 1", "Budapest", "1000", "Hungary", capacity);

    public static Event NewDraftEvent(Guid locationId, string title = "Test Concert")
        => new(
            title,
            "description",
            EventCategory.Concert,
            DateTimeOffset.UtcNow.AddDays(30),
            DateTimeOffset.UtcNow.AddDays(30).AddHours(3),
            locationId);

    public static Event NewOnSaleEvent(
        Guid locationId,
        out TicketCategory category,
        string title = "Test Concert",
        int quantity = 50,
        int locationCapacity = 500,
        decimal price = 20m)
    {
        Event ev = NewDraftEvent(locationId, title);
        category = ev.AddTicketCategory("Standard", new Money(price, "EUR"), quantity, locationCapacity);
        ev.PutOnSale();
        return ev;
    }
}
