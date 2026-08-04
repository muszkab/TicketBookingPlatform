using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Locations;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Locations.Commands.UpdateLocation;

public sealed class UpdateLocationCommandHandler
{
    private readonly IApplicationDbContext _context;

    public UpdateLocationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task HandleAsync(
        UpdateLocationCommand command,
        CancellationToken cancellationToken = default)
    {
        Location? location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Location), command.Id);

        bool duplicateExists = await _context.Locations
            .AsNoTracking()
            .AnyAsync(
                l => l.Id != command.Id
                     && l.Name.ToLower() == command.Name.ToLower()
                     && l.City.ToLower() == command.City.ToLower()
                     && l.Country.ToLower() == command.Country.ToLower(),
                cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictException(
                $"A location with name '{command.Name}' already exists in {command.City}, {command.Country}.");
        }

        if (command.Capacity < location.Capacity)
        {
            int maxAllocatedForAnyEvent = await _context.Events
                .AsNoTracking()
                .Where(e => e.LocationId == command.Id)
                .Select(e => e.TicketCategories.Sum(tc => tc.TotalQuantity))
                .DefaultIfEmpty(0)
                .MaxAsync(cancellationToken);

            if (command.Capacity < maxAllocatedForAnyEvent)
            {
                throw new ConflictException(
                    $"Cannot reduce capacity to {command.Capacity}: an existing event has {maxAllocatedForAnyEvent} tickets allocated at this location.");
            }
        }

        location.UpdateDetails(
            command.Name,
            command.Street,
            command.City,
            command.PostalCode,
            command.Country,
            command.Capacity);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
