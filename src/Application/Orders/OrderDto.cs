using Domain.Orders;
using System;
using System.Collections.Generic;

namespace Application.Orders;

public sealed record OrderDto(
    Guid Id,
    Guid UserId,
    Guid EventId,
    string EventTitle,
    OrderStatus Status,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PaidAt,
    DateTimeOffset? CancelledAt,
    IReadOnlyList<OrderItemDto> Items);
