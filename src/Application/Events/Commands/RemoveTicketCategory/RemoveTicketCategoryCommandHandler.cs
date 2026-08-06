using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Events;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events.Commands.RemoveTicketCategory;

public sealed class RemoveTicketCategoryCommandHandler
{
    private readonly IApplicationDbContext _context;

    public RemoveTicketCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task HandleAsync(
        RemoveTicketCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var targetEvent = await _context.Events
            .Include(e => e.TicketCategories)
            .FirstOrDefaultAsync(e => e.Id == command.EventId, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), command.EventId);

        targetEvent.RemoveTicketCategory(command.TicketCategoryId);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
