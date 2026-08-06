using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Events;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events.Commands.PublishEvent;

public sealed class PublishEventCommandHandler
{
    private readonly IApplicationDbContext _context;

    public PublishEventCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task HandleAsync(PublishEventCommand command, CancellationToken cancellationToken = default)
    {
        var targetEvent = await _context.Events
            .FirstOrDefaultAsync(e => e.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), command.Id);

        targetEvent.PutOnSale();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
