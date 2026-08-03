using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Locations;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Locations.Commands.CreateLocation;

public sealed class CreateLocationCommandHandler
{
    private readonly IApplicationDbContext _context;

    public CreateLocationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LocationDto> HandleAsync(
        CreateLocationCommand command,
        CancellationToken cancellationToken = default)
    {
        bool duplicateExists = await _context.Locations
            .AsNoTracking()
            .AnyAsync(
                l => l.Name.ToLower() == command.Name.ToLower()
                     && l.City.ToLower() == command.City.ToLower()
                     && l.Country.ToLower() == command.Country.ToLower(),
                cancellationToken);

        if (duplicateExists)
        {
            throw new ConflictException(
                $"A location with name '{command.Name}' already exists in {command.City}, {command.Country}.");
        }

        var location = new Location(
            command.Name,
            command.Street,
            command.City,
            command.PostalCode,
            command.Country,
            command.Capacity);

        _context.Locations.Add(location);
        await _context.SaveChangesAsync(cancellationToken);

        return new LocationDto(
            location.Id,
            location.Name,
            location.Street,
            location.City,
            location.PostalCode,
            location.Country,
            location.Capacity);
    }
}
