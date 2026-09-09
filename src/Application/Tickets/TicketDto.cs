using System;
using Domain.Tickets;

namespace Application.Tickets;

public sealed record TicketDto(
    Guid Id,
    Guid OrderId,
    Guid EventId,
    string EventTitle,
    DateTimeOffset StartsAt,
    string Location,
    Guid TicketCategoryId,
    string TicketCategoryName,
    string Code,
    TicketStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UsedAt);
