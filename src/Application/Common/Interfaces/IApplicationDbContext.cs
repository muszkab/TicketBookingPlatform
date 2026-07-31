using System.Threading;
using System.Threading.Tasks;
using Domain.Events;
using Domain.Locations;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Event> Events { get; }
    DbSet<TicketCategory> TicketCategories { get; }
    DbSet<Location> Locations { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
