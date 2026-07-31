using Domain.Locations;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedDataAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        await SeedLocationsAsync(context, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
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
    }
}
