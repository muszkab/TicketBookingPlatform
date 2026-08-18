using Application.Common.Exceptions;
using Application.Events.Commands.AddTicketCategory;
using Application.Tests.Common;
using System;
using System.Threading.Tasks;

namespace Application.Tests.Events.Commands;

public class AddTicketCategoryCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Throw_NotFound_When_EventMissing()
    {
        using var db = TestDbContextFactory.CreateInMemory();
        var handler = new AddTicketCategoryCommandHandler(db);
        await FluentActions.Invoking(() => handler.HandleAsync(
                new AddTicketCategoryCommand(Guid.NewGuid(), "VIP", 20m, "EUR", 5)))
            .Should().ThrowAsync<NotFoundException>();
    }
}
