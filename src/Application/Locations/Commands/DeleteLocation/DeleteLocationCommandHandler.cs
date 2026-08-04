using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Locations;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Locations.Commands.DeleteLocation;

public sealed class DeleteLocationCommandHandler
{
    private readonly IApplicationDbContext _context;

    public DeleteLocationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task HandleAsync(
        DeleteLocationCommand command,
        CancellationToken cancellationToken = default)
    {
        Location? location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Location), command.Id);

        bool hasEvents = await _context.Events
            .AsNoTracking()
            .AnyAsync(e => e.LocationId == command.Id, cancellationToken);

        if (hasEvents)
        {
            throw new ConflictException($"Location '{location.Name}' cannot be deleted because it has associated events.");
        }

        _context.Locations.Remove(location);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
