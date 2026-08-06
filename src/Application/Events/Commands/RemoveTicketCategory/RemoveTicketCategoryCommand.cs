using System;

namespace Application.Events.Commands.RemoveTicketCategory;

public sealed record RemoveTicketCategoryCommand(Guid EventId, Guid TicketCategoryId);
