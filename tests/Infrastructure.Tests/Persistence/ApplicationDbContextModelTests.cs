using Domain.Common;
using Domain.Events;
using Domain.Locations;
using Domain.Orders;
using Domain.Tickets;
using Domain.Users;
using Infrastructure.Persistence;
using Infrastructure.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Infrastructure.Tests.Persistence;

public class ApplicationDbContextModelTests : IDisposable
{
    private readonly ApplicationDbContext _context = TestDbContextFactory.CreateForModelInspection();

    private IEntityType Entity<TEntity>() where TEntity : class
        => _context.Model.FindEntityType(typeof(TEntity))
           ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not part of the model.");

    public void Dispose() => _context.Dispose();

    public static IEnumerable<object[]> MappedEntities()
    {
        yield return new object[] { typeof(Event), "Events" };
        yield return new object[] { typeof(TicketCategory), "TicketCategories" };
        yield return new object[] { typeof(Location), "Locations" };
        yield return new object[] { typeof(User), "Users" };
        yield return new object[] { typeof(Order), "Orders" };
        yield return new object[] { typeof(OrderItem), "OrderItems" };
        yield return new object[] { typeof(Ticket), "Tickets" };
    }

    [Theory]
    [MemberData(nameof(MappedEntities))]
    public void Model_Should_Map_Entity_To_ExpectedTable(Type clrType, string tableName)
    {
        IEntityType? entityType = _context.Model.FindEntityType(clrType);

        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be(tableName);
    }

    [Theory]
    [MemberData(nameof(MappedEntities))]
    public void Model_Should_Never_Generate_EntityIds(Type clrType, string _)
    {
        IProperty id = _context.Model.FindEntityType(clrType)!.FindProperty(nameof(Domain.Common.Entity.Id))!;

        id.ValueGenerated.Should().Be(ValueGenerated.Never);
        id.IsPrimaryKey().Should().BeTrue();
    }

    [Fact]
    public void Model_Should_Not_Contain_UnexpectedEntities()
    {
        IEnumerable<string?> tables = _context.Model.GetEntityTypes()
            .Where(e => !e.IsOwned())
            .Select(e => e.GetTableName());

        tables.Should().BeEquivalentTo(
            "Events", "TicketCategories", "Locations", "Users", "Orders", "OrderItems", "Tickets");
    }

    [Fact]
    public void EventConfiguration_Should_Apply_ColumnConstraints()
    {
        IEntityType eventType = Entity<Event>();

        eventType.FindProperty(nameof(Event.Title))!.IsNullable.Should().BeFalse();
        eventType.FindProperty(nameof(Event.Title))!.GetMaxLength().Should().Be(200);
        eventType.FindProperty(nameof(Event.Description))!.GetMaxLength().Should().Be(2000);
        eventType.FindProperty(nameof(Event.StartsAt))!.IsNullable.Should().BeFalse();
        eventType.FindProperty(nameof(Event.EndsAt))!.IsNullable.Should().BeFalse();
        eventType.FindProperty(nameof(Event.LocationId))!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void EventConfiguration_Should_Store_EnumsAsStrings()
    {
        IEntityType eventType = Entity<Event>();

        eventType.FindProperty(nameof(Event.Category))!.GetProviderClrType().Should().Be<string>();
        eventType.FindProperty(nameof(Event.Category))!.GetMaxLength().Should().Be(50);
        eventType.FindProperty(nameof(Event.Status))!.GetProviderClrType().Should().Be<string>();
        eventType.FindProperty(nameof(Event.Status))!.GetMaxLength().Should().Be(50);
    }

    [Fact]
    public void EventConfiguration_Should_Define_Indexes()
    {
        IEntityType eventType = Entity<Event>();

        eventType.GetIndexes().Should().Contain(i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(Event.LocationId));
        eventType.GetIndexes().Should().Contain(i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(Event.StartsAt));
    }

    [Fact]
    public void EventConfiguration_Should_Restrict_LocationDeletion()
    {
        IForeignKey fk = Entity<Event>().GetForeignKeys()
            .Single(f => f.PrincipalEntityType.ClrType == typeof(Location));

        fk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }

    [Fact]
    public void EventConfiguration_Should_Cascade_TicketCategories_And_UseFieldAccess()
    {
        INavigation navigation = Entity<Event>().FindNavigation(nameof(Event.TicketCategories))!;

        navigation.GetPropertyAccessMode().Should().Be(PropertyAccessMode.Field);
        navigation.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
        navigation.ForeignKey.Properties.Should().ContainSingle()
            .Which.Name.Should().Be(nameof(TicketCategory.EventId));
    }

    [Fact]
    public void TicketCategoryConfiguration_Should_Apply_ColumnConstraints_And_Index()
    {
        IEntityType type = Entity<TicketCategory>();

        type.FindProperty(nameof(TicketCategory.Name))!.IsNullable.Should().BeFalse();
        type.FindProperty(nameof(TicketCategory.Name))!.GetMaxLength().Should().Be(100);
        type.FindProperty(nameof(TicketCategory.TotalQuantity))!.IsNullable.Should().BeFalse();
        type.FindProperty(nameof(TicketCategory.AvailableQuantity))!.IsNullable.Should().BeFalse();
        type.GetIndexes().Should().Contain(i =>
            i.Properties.Count == 1 && i.Properties[0].Name == nameof(TicketCategory.EventId));
    }

    [Fact]
    public void TicketCategoryConfiguration_Should_Own_PriceMoney()
    {
        INavigation price = Entity<TicketCategory>().FindNavigation(nameof(TicketCategory.Price))!;

        price.TargetEntityType.IsOwned().Should().BeTrue();
        price.TargetEntityType.FindProperty(nameof(Money.Amount))!.GetColumnName().Should().Be("PriceAmount");
        price.TargetEntityType.FindProperty(nameof(Money.Amount))!.GetColumnType().Should().Be("decimal(18,2)");
        price.TargetEntityType.FindProperty(nameof(Money.Currency))!.GetColumnName().Should().Be("PriceCurrency");
        price.TargetEntityType.FindProperty(nameof(Money.Currency))!.GetMaxLength().Should().Be(3);
    }

    [Fact]
    public void LocationConfiguration_Should_Apply_ColumnConstraints()
    {
        IEntityType type = Entity<Location>();

        type.FindProperty(nameof(Location.Name))!.GetMaxLength().Should().Be(200);
        type.FindProperty(nameof(Location.City))!.GetMaxLength().Should().Be(100);
        type.FindProperty(nameof(Location.Country))!.GetMaxLength().Should().Be(100);
        type.FindProperty(nameof(Location.Street))!.GetMaxLength().Should().Be(200);
        type.FindProperty(nameof(Location.PostalCode))!.GetMaxLength().Should().Be(20);
        type.FindProperty(nameof(Location.Capacity))!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void LocationConfiguration_Should_Define_CompositeUniqueIndex()
    {
        IIndex index = Entity<Location>().GetIndexes().Single(i => i.IsUnique);

        index.Properties.Select(p => p.Name).Should().Equal(
            nameof(Location.Name), nameof(Location.City), nameof(Location.Country));
    }

    [Fact]
    public void UserConfiguration_Should_Apply_ColumnConstraints()
    {
        IEntityType type = Entity<User>();

        type.FindProperty(nameof(User.Email))!.GetMaxLength().Should().Be(200);
        type.FindProperty(nameof(User.PasswordHash))!.GetMaxLength().Should().Be(512);
        type.FindProperty(nameof(User.FullName))!.GetMaxLength().Should().Be(200);
        type.FindProperty(nameof(User.CreatedAt))!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void UserConfiguration_Should_Store_RoleAsString_And_UniqueEmail()
    {
        IEntityType type = Entity<User>();

        IProperty role = type.FindProperty(nameof(User.Role))!;
        role.GetProviderClrType().Should().Be<string>();
        role.GetMaxLength().Should().Be(32);

        type.GetIndexes().Single(i => i.IsUnique).Properties
            .Should().ContainSingle().Which.Name.Should().Be(nameof(User.Email));
    }

    [Fact]
    public void OrderConfiguration_Should_Store_StatusAsString_And_Define_Indexes()
    {
        IEntityType type = Entity<Order>();

        IProperty status = type.FindProperty(nameof(Order.Status))!;
        status.GetProviderClrType().Should().Be<string>();
        status.GetMaxLength().Should().Be(50);
        status.IsNullable.Should().BeFalse();

        type.GetIndexes().Should().Contain(i => i.Properties[0].Name == nameof(Order.UserId));
        type.GetIndexes().Should().Contain(i => i.Properties[0].Name == nameof(Order.EventId));
    }

    [Fact]
    public void OrderConfiguration_Should_Map_OptionalTimestamps()
    {
        IEntityType type = Entity<Order>();

        type.FindProperty(nameof(Order.CreatedAt))!.IsNullable.Should().BeFalse();
        type.FindProperty(nameof(Order.PaidAt))!.IsNullable.Should().BeTrue();
        type.FindProperty(nameof(Order.CancelledAt))!.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void OrderConfiguration_Should_Own_TotalAmount()
    {
        INavigation total = Entity<Order>().FindNavigation(nameof(Order.TotalAmount))!;

        total.TargetEntityType.IsOwned().Should().BeTrue();
        total.TargetEntityType.FindProperty(nameof(Money.Amount))!.GetColumnName().Should().Be("TotalAmount");
        total.TargetEntityType.FindProperty(nameof(Money.Amount))!.GetColumnType().Should().Be("decimal(18,2)");
        total.TargetEntityType.FindProperty(nameof(Money.Currency))!.GetColumnName().Should().Be("TotalCurrency");
        total.TargetEntityType.FindProperty(nameof(Money.Currency))!.GetMaxLength().Should().Be(3);
    }

    [Fact]
    public void OrderConfiguration_Should_Restrict_EventDeletion_And_Cascade_Items()
    {
        IEntityType type = Entity<Order>();

        type.FindNavigation(nameof(Order.Event))!.ForeignKey.DeleteBehavior
            .Should().Be(DeleteBehavior.Restrict);

        INavigation items = type.FindNavigation(nameof(Order.Items))!;
        items.GetPropertyAccessMode().Should().Be(PropertyAccessMode.Field);
        items.ForeignKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    [Fact]
    public void OrderItemConfiguration_Should_Own_UnitPrice_And_Ignore_LineTotal()
    {
        IEntityType type = Entity<OrderItem>();

        type.FindProperty(nameof(OrderItem.LineTotal)).Should().BeNull();
        type.FindNavigation(nameof(OrderItem.LineTotal)).Should().BeNull();

        INavigation unitPrice = type.FindNavigation(nameof(OrderItem.UnitPrice))!;
        unitPrice.TargetEntityType.FindProperty(nameof(Money.Amount))!.GetColumnName().Should().Be("UnitPriceAmount");
        unitPrice.TargetEntityType.FindProperty(nameof(Money.Currency))!.GetColumnName().Should().Be("UnitPriceCurrency");
    }

    [Fact]
    public void OrderItemConfiguration_Should_Define_Indexes_And_Restrict_TicketCategory()
    {
        IEntityType type = Entity<OrderItem>();

        type.GetIndexes().Should().Contain(i => i.Properties[0].Name == nameof(OrderItem.OrderId));
        type.GetIndexes().Should().Contain(i => i.Properties[0].Name == nameof(OrderItem.TicketCategoryId));

        type.FindNavigation(nameof(OrderItem.TicketCategory))!.ForeignKey.DeleteBehavior
            .Should().Be(DeleteBehavior.Restrict);
    }

    [Fact]
    public void TicketConfiguration_Should_Define_UniqueCodeIndex()
    {
        IEntityType type = Entity<Ticket>();

        type.FindProperty(nameof(Ticket.Code))!.GetMaxLength().Should().Be(64);
        type.GetIndexes().Single(i => i.IsUnique).Properties
            .Should().ContainSingle().Which.Name.Should().Be(nameof(Ticket.Code));
    }

    [Fact]
    public void TicketConfiguration_Should_Store_StatusAsString_And_Restrict_AllRelations()
    {
        IEntityType type = Entity<Ticket>();

        type.FindProperty(nameof(Ticket.Status))!.GetProviderClrType().Should().Be<string>();
        type.FindProperty(nameof(Ticket.UsedAt))!.IsNullable.Should().BeTrue();

        type.GetForeignKeys().Should().OnlyContain(fk => fk.DeleteBehavior == DeleteBehavior.Restrict);
        type.GetForeignKeys().Select(fk => fk.PrincipalEntityType.ClrType)
            .Should().BeEquivalentTo(new[] { typeof(Order), typeof(Event), typeof(TicketCategory) });
    }
}
