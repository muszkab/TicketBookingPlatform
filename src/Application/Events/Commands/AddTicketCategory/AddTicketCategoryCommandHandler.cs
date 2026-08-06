using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Domain.Common;
using Domain.Events;
using Domain.Locations;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events.Commands.AddTicketCategory;

public sealed class AddTicketCategoryCommandHandler
{
    private readonly IApplicationDbContext _context;

    public AddTicketCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TicketCategoryDto> HandleAsync(AddTicketCategoryCommand command, CancellationToken cancellationToken = default)
    {
        var targetEvent = await _context.Events
            .Include(e => e.TicketCategories)
            .Include(e => e.Location)
            .FirstOrDefaultAsync(e => e.Id == command.EventId, cancellationToken)
            ?? throw new NotFoundException(nameof(Event), command.EventId);

        if (targetEvent.Location is null)
            throw new NotFoundException(nameof(Location), targetEvent.LocationId);

        var price = new Money(command.Price, command.Currency);
        var category = targetEvent.AddTicketCategory(command.Name, price, command.Quantity, targetEvent.Location.Capacity);

        await _context.SaveChangesAsync(cancellationToken);

        return new TicketCategoryDto(
            category.Id,
            category.Name,
            category.Price.Amount,
            category.Price.Currency,
            category.TotalQuantity,
            category.AvailableQuantity);
    }
}
