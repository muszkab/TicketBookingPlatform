using Domain.Events;
using Domain.Locations;
using Domain.Orders;
using Domain.Tickets;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Event> Events { get; }
    DbSet<TicketCategory> TicketCategories { get; }
    DbSet<Location> Locations { get; }
    DbSet<User> Users { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Ticket> Tickets { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
