using Domain.Events;
using System;

namespace WebApi.Contracts.Events;

public sealed record CreateEventRequest(
    string Title,
    string Description,
    EventCategory Category,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    Guid LocationId);
