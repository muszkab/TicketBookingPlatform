using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Events;
using Domain.Locations;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events.Commands.CreateEvent;

public sealed class CreateEventCommandHandler
{
    private readonly IApplicationDbContext _context;

    public CreateEventCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EventDto> HandleAsync(
        CreateEventCommand command,
        CancellationToken cancellationToken = default)
    {
        bool locationExists = await _context.Locations
            .AsNoTracking()
            .AnyAsync(l => l.Id == command.LocationId, cancellationToken);

        if (!locationExists)
            throw new NotFoundException(nameof(Location), command.LocationId);

        var newEvent = new Event(
            command.Title,
            command.Description,
            command.Category,
            command.StartsAt,
            command.EndsAt,
            command.LocationId);

        _context.Events.Add(newEvent);
        await _context.SaveChangesAsync(cancellationToken);

        return new EventDto(
            newEvent.Id,
            newEvent.Title,
            newEvent.Description,
            newEvent.Category,
            newEvent.Status,
            newEvent.StartsAt,
            newEvent.EndsAt,
            newEvent.LocationId,
            new List<TicketCategoryDto>());
    }
}
