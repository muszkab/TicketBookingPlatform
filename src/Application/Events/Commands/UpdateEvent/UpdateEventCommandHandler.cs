using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Events;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events.Commands.UpdateEvent;

public sealed class UpdateEventCommandHandler
{
    private readonly IApplicationDbContext _context;

    public UpdateEventCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task HandleAsync(
        UpdateEventCommand command,
        CancellationToken cancellationToken = default)
    {
        var targetEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), command.Id);

        targetEvent.UpdateDetails(
            command.Title,
            command.Description,
            command.Category,
            command.StartsAt,
            command.EndsAt);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
