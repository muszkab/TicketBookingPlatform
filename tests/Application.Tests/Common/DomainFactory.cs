using Domain.Common;
using Domain.Events;
using Domain.Locations;
using Domain.Users;
using System;

namespace Application.Tests.Common;

internal static class DomainFactory
{
    public static User NewUser(string email = "user@example.com", string passwordHash = "hash", string fullName = "Test User", UserRole role = UserRole.Customer)
        => new(email, passwordHash, fullName, role);

    public static Location NewLocation(int capacity = 100)
        => new("Arena", "Street 1", "City", "1234", "Country", capacity);

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
        decimal price = 10m)
    {
        var ev = NewDraftEvent(locationId);
        category = ev.AddTicketCategory("Std", new Money(price, "EUR"), categoryQuantity, locationCapacity);
        ev.PutOnSale();
        return ev;
    }
}
