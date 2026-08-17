using System;

namespace Application.Orders;

public sealed record OrderItemDto(
    Guid Id,
    Guid TicketCategoryId,
    string TicketCategoryName,
    int Quantity,
    decimal UnitPriceAmount,
    string Currency,
    decimal LineTotalAmount);
