using System;

namespace Application.Events;

public sealed record TicketCategoryDto(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    int TotalQuantity,
    int AvailableQuantity);
