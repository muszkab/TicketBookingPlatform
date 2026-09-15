using Domain.Common;
using Domain.Events;
using Domain.Locations;
using Domain.Orders;
using Domain.Tickets;
using Domain.Users;
using Infrastructure.Persistence;
using Infrastructure.Tests.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Tests.Persistence;

public class ApplicationDbContextPersistenceTests : IDisposable
{
    private readonly SqliteTestDatabase _database = SqliteTestDatabase.Create();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Should_Persist_And_Reload_Location()
    {
        Location location = DomainFactory.NewLocation();

        await using (ApplicationDbContext write = _database.CreateContext())
        {
            write.Locations.Add(location);
            await write.SaveChangesAsync();
        }

        await using ApplicationDbContext read = _database.CreateContext();
        Location? reloaded = await read.Locations.SingleOrDefaultAsync(l => l.Id == location.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Name.Should().Be(location.Name);
        reloaded.Capacity.Should().Be(location.Capacity);
    }

    [Fact]
    public async Task Should_Preserve_ClientGeneratedId()
    {
        Location location = DomainFactory.NewLocation();
        Guid expectedId = location.Id;

        await using ApplicationDbContext context = _database.CreateContext();
        context.Locations.Add(location);
        await context.SaveChangesAsync();

        location.Id.Should().Be(expectedId);
        expectedId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Should_Enforce_LocationUniqueIndex()
    {
        await using ApplicationDbContext context = _database.CreateContext();
        context.Locations.Add(DomainFactory.NewLocation(name: "Arena", city: "Budapest", country: "Hungary"));
        await context.SaveChangesAsync();

        context.Locations.Add(DomainFactory.NewLocation(name: "Arena", city: "Budapest", country: "Hungary"));

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Should_Allow_SameLocationName_InDifferentCity()
    {
        await using ApplicationDbContext context = _database.CreateContext();
        context.Locations.Add(DomainFactory.NewLocation(name: "Arena", city: "Budapest", country: "Hungary"));
        context.Locations.Add(DomainFactory.NewLocation(name: "Arena", city: "Debrecen", country: "Hungary"));

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_Enforce_UserEmailUniqueIndex()
    {
        await using ApplicationDbContext context = _database.CreateContext();
        context.Users.Add(DomainFactory.NewUser(email: "dup@example.com"));
        await context.SaveChangesAsync();

        context.Users.Add(DomainFactory.NewUser(email: "DUP@Example.com"));

        await FluentActions.Invoking(() => context.SaveChangesAsync())
            .Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Should_Persist_EnumsAsStrings()
    {
        Location location = DomainFactory.NewLocation();
        Event ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out _);
        User user = DomainFactory.NewUser(role: UserRole.Admin);

        await using (ApplicationDbContext write = _database.CreateContext())
        {
            write.Locations.Add(location);
            write.Events.Add(ev);
            write.Users.Add(user);
            await write.SaveChangesAsync();
        }

        await using ApplicationDbContext read = _database.CreateContext();
        string eventStatus = await read.Database
            .SqlQueryRaw<string>("SELECT Status AS Value FROM Events LIMIT 1").SingleAsync();
        string userRole = await read.Database
            .SqlQueryRaw<string>("SELECT Role AS Value FROM Users LIMIT 1").SingleAsync();

        eventStatus.Should().Be(nameof(EventStatus.OnSale));
        userRole.Should().Be(nameof(UserRole.Admin));
    }

    [Fact]
    public async Task Should_Persist_TicketCategories_AsOwnedMoneyColumns()
    {
        Location location = DomainFactory.NewLocation();
        Event ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out TicketCategory category, price: 1234.56m);

        await using (ApplicationDbContext write = _database.CreateContext())
        {
            write.Locations.Add(location);
            write.Events.Add(ev);
            await write.SaveChangesAsync();
        }

        await using ApplicationDbContext read = _database.CreateContext();
        TicketCategory reloaded = await read.TicketCategories.SingleAsync(tc => tc.Id == category.Id);

        reloaded.Price.Amount.Should().Be(1234.56m);
        reloaded.Price.Currency.Should().Be("EUR");
    }

    [Fact]
    public async Task Should_Load_TicketCategories_ThroughBackingField()
    {
        Location location = DomainFactory.NewLocation();
        Event ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out _);

        await using (ApplicationDbContext write = _database.CreateContext())
        {
            write.Locations.Add(location);
            write.Events.Add(ev);
            await write.SaveChangesAsync();
        }

        await using ApplicationDbContext read = _database.CreateContext();
        Event reloaded = await read.Events
            .Include(e => e.TicketCategories)
            .SingleAsync(e => e.Id == ev.Id);

        reloaded.TicketCategories.Should().ContainSingle();
    }

    [Fact]
    public async Task Should_Cascade_Delete_TicketCategories_WithEvent()
    {
        Location location = DomainFactory.NewLocation();
        Event ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out _);

        await using (ApplicationDbContext write = _database.CreateContext())
        {
            write.Locations.Add(location);
            write.Events.Add(ev);
            await write.SaveChangesAsync();
        }

        await using ApplicationDbContext delete = _database.CreateContext();
        Event tracked = await delete.Events.Include(e => e.TicketCategories).SingleAsync(e => e.Id == ev.Id);
        delete.Events.Remove(tracked);
        await delete.SaveChangesAsync();

        await using ApplicationDbContext read = _database.CreateContext();
        (await read.TicketCategories.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Should_Restrict_LocationDeletion_WhenEventExists()
    {
        Location location = DomainFactory.NewLocation();
        Event ev = DomainFactory.NewDraftEvent(location.Id);

        await using (ApplicationDbContext write = _database.CreateContext())
        {
            write.Locations.Add(location);
            write.Events.Add(ev);
            await write.SaveChangesAsync();
        }

        await using ApplicationDbContext delete = _database.CreateContext();
        delete.Locations.Remove(await delete.Locations.SingleAsync(l => l.Id == location.Id));

        await FluentActions.Invoking(() => delete.SaveChangesAsync())
            .Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Should_Persist_Order_WithItems_And_TotalAmount()
    {
        Location location = DomainFactory.NewLocation();
        Event ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out TicketCategory category, price: 25m);
        var userId = Guid.NewGuid();
        Order order = DomainFactory.NewPendingOrder(userId, ev, category, quantity: 3);

        await using (ApplicationDbContext write = _database.CreateContext())
        {
            write.Locations.Add(location);
            write.Events.Add(ev);
            write.Orders.Add(order);
            await write.SaveChangesAsync();
        }

        await using ApplicationDbContext read = _database.CreateContext();
        Order reloaded = await read.Orders.Include(o => o.Items).SingleAsync(o => o.Id == order.Id);

        reloaded.UserId.Should().Be(userId);
        reloaded.Status.Should().Be(OrderStatus.Pending);
        reloaded.TotalAmount.Should().Be(new Money(75m, "EUR"));
        reloaded.Items.Should().ContainSingle()
            .Which.Quantity.Should().Be(3);
    }

    [Fact]
    public async Task Should_Compute_LineTotal_AfterReload_WithoutBeingPersisted()
    {
        Location location = DomainFactory.NewLocation();
        Event ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out TicketCategory category, price: 25m);
        Order order = DomainFactory.NewPendingOrder(Guid.NewGuid(), ev, category, quantity: 4);

        await using (ApplicationDbContext write = _database.CreateContext())
        {
            write.Locations.Add(location);
            write.Events.Add(ev);
            write.Orders.Add(order);
            await write.SaveChangesAsync();
        }

        await using ApplicationDbContext read = _database.CreateContext();
        OrderItem item = await read.OrderItems.SingleAsync();

        item.LineTotal.Amount.Should().Be(100m);
    }

    [Fact]
    public async Task Should_Cascade_Delete_OrderItems_WithOrder()
    {
        Location location = DomainFactory.NewLocation();
        Event ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out TicketCategory category);
        Order order = DomainFactory.NewPendingOrder(Guid.NewGuid(), ev, category);

        await using (ApplicationDbContext write = _database.CreateContext())
        {
            write.Locations.Add(location);
            write.Events.Add(ev);
            write.Orders.Add(order);
            await write.SaveChangesAsync();
        }

        await using ApplicationDbContext delete = _database.CreateContext();
        delete.Orders.Remove(await delete.Orders.Include(o => o.Items).SingleAsync(o => o.Id == order.Id));
        await delete.SaveChangesAsync();

        await using ApplicationDbContext read = _database.CreateContext();
        (await read.OrderItems.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Should_Persist_Tickets_WithUniqueCodes()
    {
        Location location = DomainFactory.NewLocation();
        Event ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out TicketCategory category);
        Order order = DomainFactory.NewPendingOrder(Guid.NewGuid(), ev, category, quantity: 2);

        await using ApplicationDbContext context = _database.CreateContext();
        context.Locations.Add(location);
        context.Events.Add(ev);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        IReadOnlyList<Ticket> tickets = order.Pay(ev.Status);
        context.Tickets.AddRange(tickets);
        await context.SaveChangesAsync();

        await using ApplicationDbContext read = _database.CreateContext();
        List<Ticket> reloaded = await read.Tickets.ToListAsync();

        reloaded.Should().HaveCount(2);
        reloaded.Select(t => t.Code).Distinct().Should().HaveCount(2);
        reloaded.Should().OnlyContain(t => t.Status == TicketStatus.Valid);
    }

    [Fact]
    public async Task Should_Restrict_OrderDeletion_WhenTicketsExist()
    {
        Location location = DomainFactory.NewLocation();
        Event ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out TicketCategory category);
        Order order = DomainFactory.NewPendingOrder(Guid.NewGuid(), ev, category, quantity: 1);

        await using (ApplicationDbContext write = _database.CreateContext())
        {
            write.Locations.Add(location);
            write.Events.Add(ev);
            write.Orders.Add(order);
            await write.SaveChangesAsync();
            write.Tickets.AddRange(order.Pay(ev.Status));
            await write.SaveChangesAsync();
        }

        await using ApplicationDbContext delete = _database.CreateContext();
        delete.Orders.Remove(await delete.Orders.SingleAsync(o => o.Id == order.Id));

        await FluentActions.Invoking(() => delete.SaveChangesAsync())
            .Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Should_Support_Navigation_From_Order_To_Event()
    {
        Location location = DomainFactory.NewLocation();
        Event ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out TicketCategory category);
        Order order = DomainFactory.NewPendingOrder(Guid.NewGuid(), ev, category);

        await using (ApplicationDbContext write = _database.CreateContext())
        {
            write.Locations.Add(location);
            write.Events.Add(ev);
            write.Orders.Add(order);
            await write.SaveChangesAsync();
        }

        await using ApplicationDbContext read = _database.CreateContext();
        Order reloaded = await read.Orders
            .Include(o => o.Event)!
            .ThenInclude(e => e!.Location)
            .SingleAsync(o => o.Id == order.Id);

        reloaded.Event.Should().NotBeNull();
        reloaded.Event!.Title.Should().Be(ev.Title);
        reloaded.Event.Location.Should().NotBeNull();
    }
}
