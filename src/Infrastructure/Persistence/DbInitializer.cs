using Domain.Common;
using Domain.Events;
using Domain.Locations;
using Domain.Users;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedDataAsync(
        ApplicationDbContext context,
        IConfiguration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        await SeedLocationsAsync(context, cancellationToken);
        await SeedEventsAsync(context, cancellationToken);
        await SeedAdminUserAsync(context, configuration, cancellationToken);
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

        Dictionary<string, (Guid Id, int Capacity)> locationsByName = await context.Locations
            .AsNoTracking()
            .ToDictionaryAsync(l => l.Name, l => (l.Id, l.Capacity), cancellationToken);

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
                locationId: locationsByName["Művészetek Palotája"].Id),

            new(
                title: "Rockmaraton Warm-up",
                description: "Hazai rockzenekarok közös nagykoncertje a nyári fesztiválszezon nyitányaként.",
                category: EventCategory.Concert,
                startsAt: new DateTimeOffset(2025, 6, 21, 18, 0, 0, cet),
                endsAt: new DateTimeOffset(2025, 6, 21, 23, 30, 0, cet),
                locationId: locationsByName["Pick Aréna"].Id),

            new(
                title: "Az ember tragédiája",
                description: "Madách Imre klasszikusa új rendezésben, két felvonásban, szünettel.",
                category: EventCategory.Theater,
                startsAt: new DateTimeOffset(2025, 5, 8, 19, 0, 0, cet),
                endsAt: new DateTimeOffset(2025, 5, 8, 22, 15, 0, cet),
                locationId: locationsByName["Művészetek Palotája"].Id),

            new(
                title: "DevDays Hungary 2025",
                description: ".NET, felhő és AI témájú kétnapos szakmai konferencia fejlesztőknek.",
                category: EventCategory.Conference,
                startsAt: new DateTimeOffset(2025, 9, 18, 9, 0, 0, cet),
                endsAt: new DateTimeOffset(2025, 9, 19, 17, 0, 0, cet),
                locationId: locationsByName["Főnix Csarnok"].Id),

            new(
                title: "Kézilabda Magyar Kupa döntő",
                description: "A férfi kézilabda Magyar Kupa döntő mérkőzése.",
                category: EventCategory.Other,
                startsAt: new DateTimeOffset(2025, 3, 30, 17, 0, 0, cet),
                endsAt: new DateTimeOffset(2025, 3, 30, 19, 0, 0, cet),
                locationId: locationsByName["Főnix Csarnok"].Id),
        };

        AddTicketCategories(events, locationsByName);

        await context.Events.AddRangeAsync(events, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void AddTicketCategories(List<Event> events, Dictionary<string, (Guid Id, int Capacity)> locationsByName)
    {
        const string huf = "HUF";
        int capacity = 0;

        Event matePassio = events.Single(e => e.Title == "Bach: Máté-passió");
        capacity = locationsByName.Values.First(v => v.Id == matePassio.LocationId).Capacity;
        matePassio.AddTicketCategory("Karzat", new Money(4_900m, huf), 400, capacity);
        matePassio.AddTicketCategory("Erkély", new Money(8_500m, huf), 600, capacity);
        matePassio.AddTicketCategory("Földszint", new Money(12_900m, huf), 699, capacity);

        Event rockmaraton = events.Single(e => e.Title == "Rockmaraton Warm-up");
        capacity = locationsByName.Values.First(v => v.Id == rockmaraton.LocationId).Capacity;
        rockmaraton.AddTicketCategory("Állóhely", new Money(9_990m, huf), 8_500, capacity);
        rockmaraton.AddTicketCategory("VIP", new Money(24_990m, huf), 500, capacity);

        Event tragedia = events.Single(e => e.Title == "Az ember tragédiája");
        capacity = locationsByName.Values.First(v => v.Id == tragedia.LocationId).Capacity;
        tragedia.AddTicketCategory("Egységes helyár", new Money(6_500m, huf), 1_699, capacity);

        Event devDays = events.Single(e => e.Title == "DevDays Hungary 2025");
        capacity = locationsByName.Values.First(v => v.Id == devDays.LocationId).Capacity;
        devDays.AddTicketCategory("Early Bird", new Money(89_000m, huf), 300, capacity);
        devDays.AddTicketCategory("Standard", new Money(129_000m, huf), 900, capacity);
        devDays.AddTicketCategory("Corporate", new Money(179_000m, huf), 200, capacity);

        Event kupaDonto = events.Single(e => e.Title == "Kézilabda Magyar Kupa döntő");
        capacity = locationsByName.Values.First(v => v.Id == kupaDonto.LocationId).Capacity;
        kupaDonto.AddTicketCategory("B kategória", new Money(3_500m, huf), 4_000, capacity);
        kupaDonto.AddTicketCategory("A kategória", new Money(6_900m, huf), 2_500, capacity);
    }

    private static async Task SeedAdminUserAsync(ApplicationDbContext context, IConfiguration? configuration, CancellationToken cancellationToken)
    {
        string email = configuration?["Seed:Admin:Email"]
            ?? throw new InvalidOperationException(
                "Seed:Admin:Email is not configured. Set it via user secrets or environment variables.");
        string password = configuration?["Seed:Admin:Password"]
            ?? throw new InvalidOperationException(
                "Seed:Admin:Password is not configured. Set it via user secrets or environment variables.");
        string fullName = "System Administrator";

        string normalizedEmail = email.Trim().ToLowerInvariant();

        if (await context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            return;
        }

        var admin = new User(
            email: email,
            passwordHash: PasswordHasher.Hash(password),
            fullName: fullName,
            role: UserRole.Admin);

        await context.Users.AddAsync(admin, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
