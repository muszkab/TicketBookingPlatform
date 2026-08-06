using System;
using System.Collections.Generic;
using Domain.Events;

namespace Application.Events;

public sealed record EventDto(
    Guid Id,
    string Title,
    string Description,
    EventCategory Category,
    EventStatus Status,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    Guid LocationId,
    IReadOnlyList<TicketCategoryDto> TicketCategories);
