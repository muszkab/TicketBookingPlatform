using System;
using Domain.Events;

namespace Application.Events.Commands.UpdateEvent;

public sealed record UpdateEventCommand(
    Guid Id,
    string Title,
    string Description,
    EventCategory Category,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt);
