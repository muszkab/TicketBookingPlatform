using System;

namespace Application.Events.Commands.AddTicketCategory;

public sealed record AddTicketCategoryCommand(
    Guid EventId,
    string Name,
    decimal Price,
    string Currency,
    int Quantity);
