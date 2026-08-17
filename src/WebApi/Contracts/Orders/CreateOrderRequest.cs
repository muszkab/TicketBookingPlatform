using System;
using System.Collections.Generic;

namespace WebApi.Contracts.Orders;

public sealed record CreateOrderItemRequest(
    Guid TicketCategoryId,
    int Quantity);

public sealed record CreateOrderRequest(
    Guid EventId,
    string Currency,
    IReadOnlyList<CreateOrderItemRequest> Items);
