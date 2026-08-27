using Domain.Events;
using System;

namespace Application.Events.Queries.GetEvents;

public sealed record GetEventsQuery(
    EventCategory? Category = null,
    EventStatus? Status = null,
    Guid? LocationId = null,
    int Page = 1,
    int PageSize = GetEventsQuery.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
}
