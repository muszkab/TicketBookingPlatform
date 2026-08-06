using System;
using Domain.Events;

namespace Application.Events.Commands.CreateEvent;

public sealed record CreateEventCommand(
    string Title,
    string Description,
    EventCategory Category,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    Guid LocationId);
