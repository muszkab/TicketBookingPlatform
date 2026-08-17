using System;
using System.Collections.Generic;

namespace Application.Orders.Commands.CreateOrder;

public sealed record CreateOrderItemDto(
    Guid TicketCategoryId,
    int Quantity);

public sealed record CreateOrderCommand(
    Guid EventId,
    string Currency,
    IReadOnlyList<CreateOrderItemDto> Items);
