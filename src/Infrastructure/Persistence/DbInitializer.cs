using Domain.Events;
using Domain.Locations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedDataAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        await SeedLocationsAsync(context, cancellationToken);
        await SeedEventsAsync(context, cancellationToken);
    }

    private static async Task SeedLocationsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Locations.AnyAsync(cancellationToken))
        {
            return;
        }

        var locations = new[]
        {
            new Location(
                name: "Művészetek Palotája",
                street: "Komor Marcell utca 1",
                city: "Budapest",
                postalCode: "1095",
                country: "Hungary",
                capacity: 1699),
            new Location(
                name: "Főnix Csarnok",
                street: "Kassai út 28",
                city: "Debrecen",
                postalCode: "4028",
                country: "Hungary",
                capacity: 6500),
            new Location(
                name: "Pick Aréna",
                street: "Felső Tisza-part 4",
                city: "Szeged",
                postalCode: "6726",
                country: "Hungary",
                capacity: 10000),
        };

        await context.Locations.AddRangeAsync(locations, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedEventsAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Events.AnyAsync(cancellationToken))
        {
            return;
        }

        Dictionary<string, Guid> locationsByName = await context.Locations
            .AsNoTracking()
            .ToDictionaryAsync(l => l.Name, l => l.Id, cancellationToken);

        if (locationsByName.Count == 0)
        {
            return;
        }

        // Local time in Hungary (UTC+1, no DST handling needed for seed data).
        TimeSpan cet = TimeSpan.FromHours(1);

        var events = new List<Event>
        {
            new(
                title: "Bach: Máté-passió",
                description: "A Nemzeti Filharmonikusok és a Nemzeti Énekkar előadása, vezényel Vashegyi György.",
                category: EventCategory.Concert,
                startsAt: new DateTimeOffset(2025, 4, 12, 19, 30, 0, cet),
                endsAt: new DateTimeOffset(2025, 4, 12, 22, 30, 0, cet),
                locationId: locationsByName["Művészetek Palotája"]),

            new(
                title: "Rockmaraton Warm-up",
                description: "Hazai rockzenekarok közös nagykoncertje a nyári fesztiválszezon nyitányaként.",
                category: EventCategory.Concert,
                startsAt: new DateTimeOffset(2025, 6, 21, 18, 0, 0, cet),
                endsAt: new DateTimeOffset(2025, 6, 21, 23, 30, 0, cet),
                locationId: locationsByName["Pick Aréna"]),

            new(
                title: "Az ember tragédiája",
                description: "Madách Imre klasszikusa új rendezésben, két felvonásban, szünettel.",
                category: EventCategory.Theater,
                startsAt: new DateTimeOffset(2025, 5, 8, 19, 0, 0, cet),
                endsAt: new DateTimeOffset(2025, 5, 8, 22, 15, 0, cet),
                locationId: locationsByName["Művészetek Palotája"]),

            new(
                title: "DevDays Hungary 2025",
                description: ".NET, felhő és AI témájú kétnapos szakmai konferencia fejlesztőknek.",
                category: EventCategory.Conference,
                startsAt: new DateTimeOffset(2025, 9, 18, 9, 0, 0, cet),
                endsAt: new DateTimeOffset(2025, 9, 19, 17, 0, 0, cet),
                locationId: locationsByName["Főnix Csarnok"]),

            new(
                title: "Kézilabda Magyar Kupa döntő",
                description: "A férfi kézilabda Magyar Kupa döntő mérkőzése.",
                category: EventCategory.Other,
                startsAt: new DateTimeOffset(2025, 3, 30, 17, 0, 0, cet),
                endsAt: new DateTimeOffset(2025, 3, 30, 19, 0, 0, cet),
                locationId: locationsByName["Főnix Csarnok"]),
        };

        await context.Events.AddRangeAsync(events, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
