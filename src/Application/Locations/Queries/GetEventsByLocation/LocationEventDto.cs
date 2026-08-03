using System;
using Domain.Events;

namespace Application.Locations.Queries.GetEventsByLocation;

public sealed record LocationEventDto(
    Guid Id,
    string Title,
    string Description,
    EventCategory Category,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    EventStatus Status);
