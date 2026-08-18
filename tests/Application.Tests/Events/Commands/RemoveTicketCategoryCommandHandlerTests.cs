using Application.Common.Exceptions;
using Application.Events.Commands.RemoveTicketCategory;
using Application.Tests.Common;
using System;
using System.Threading.Tasks;

namespace Application.Tests.Events.Commands;

public class RemoveTicketCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_EventMissing()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var handler = new RemoveTicketCategoryCommandHandler(db);
        await FluentActions.Invoking(() => handler.HandleAsync(
                new RemoveTicketCategoryCommand(Guid.NewGuid(), Guid.NewGuid())))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_Should_RemoveCategory()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var location = DomainFactory.NewLocation();
        var ev = DomainFactory.NewDraftEvent(location.Id);
        var category = ev.AddTicketCategory("VIP", new Domain.Common.Money(20m, "EUR"), 5, location.Capacity);
        db.Locations.Add(location);
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var handler = new RemoveTicketCategoryCommandHandler(db);
        await handler.HandleAsync(new RemoveTicketCategoryCommand(ev.Id, category.Id));

        ev.TicketCategories.Should().BeEmpty();
    }
}
