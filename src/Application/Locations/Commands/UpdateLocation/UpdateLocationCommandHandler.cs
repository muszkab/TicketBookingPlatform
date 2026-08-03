using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Locations;
using Microsoft.EntityFrameworkCore;
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
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Location), command.Id);

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
