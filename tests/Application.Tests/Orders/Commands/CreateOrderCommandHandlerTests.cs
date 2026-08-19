using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Orders.Commands.CreateOrder;
using Application.Tests.Common;
using Domain.Orders;
using Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Tests.Orders.Commands;

public class CreateOrderCommandHandlerTests
{
    private static (CreateOrderCommandHandler handler, ApplicationDbContext db, ICurrentUserService currentUser) BuildSut(Guid? userId)
    {
        var db = TestDbContextFactory.CreateInMemory();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(userId);
        return (new CreateOrderCommandHandler(db, currentUser), db, currentUser);
    }

    [Fact]
    public async Task Handle_Should_Throw_When_NoCurrentUser()
    {
        var (handler, _, _) = BuildSut(userId: null);
        var command = new CreateOrderCommand(Guid.NewGuid(), "EUR", new List<CreateOrderItemDto> { new(Guid.NewGuid(), 1) });

        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_Should_Throw_When_ItemsEmpty()
    {
        var (handler, _, _) = BuildSut(Guid.NewGuid());
        var command = new CreateOrderCommand(Guid.NewGuid(), "EUR", new List<CreateOrderItemDto>());

        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_EventMissing()
    {
        var (handler, _, _) = BuildSut(Guid.NewGuid());
        var command = new CreateOrderCommand(Guid.NewGuid(), "EUR", new List<CreateOrderItemDto> { new(Guid.NewGuid(), 1) });

        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_Throw_Conflict_When_EventNotOnSale()
    {
        var userId = Guid.NewGuid();
        var (handler, db, _) = BuildSut(userId);
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewDraftEvent(location.Id);
        db.Locations.Add(location);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var command = new CreateOrderCommand(ev.Id, "EUR", new List<CreateOrderItemDto> { new(Guid.NewGuid(), 1) });

        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_TicketCategoryUnknown()
    {
        var userId = Guid.NewGuid();
        var (handler, db, _) = BuildSut(userId);
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out _);
        db.Locations.Add(location);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var command = new CreateOrderCommand(ev.Id, "EUR", new List<CreateOrderItemDto> { new(Guid.NewGuid(), 1) });

        await FluentActions.Invoking(() => handler.HandleAsync(command))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_CreateOrder_And_ReturnDto()
    {
        var userId = Guid.NewGuid();
        var (handler, db, _) = BuildSut(userId);
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewOnSaleEventWithCategory(location.Id, out var category, price: 20m);
        db.Locations.Add(location);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var command = new CreateOrderCommand(ev.Id, "EUR", new List<CreateOrderItemDto> { new(category.Id, 3) });

        var dto = await handler.HandleAsync(command);

        dto.Status.Should().Be(OrderStatus.Pending);
        dto.UserId.Should().Be(userId);
        dto.EventId.Should().Be(ev.Id);
        dto.TotalAmount.Should().Be(60m);
        dto.Items.Should().ContainSingle().Which.Quantity.Should().Be(3);

        db.Orders.Should().HaveCount(1);
    }
}
