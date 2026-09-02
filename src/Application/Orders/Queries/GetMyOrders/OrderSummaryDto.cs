using Domain.Orders;
using System;

namespace Application.Orders.Queries.GetMyOrders;

public sealed record OrderSummaryDto(
    Guid Id,
    Guid EventId,
    string EventTitle,
    OrderStatus Status,
    decimal TotalAmount,
    string Currency,
    int ItemCount,
    DateTimeOffset CreatedAt);
