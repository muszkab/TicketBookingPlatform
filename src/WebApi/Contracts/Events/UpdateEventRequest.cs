using System;
using Domain.Events;

namespace WebApi.Contracts.Events;

public sealed record UpdateEventRequest(
    string Title,
    string Description,
    EventCategory Category,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);
