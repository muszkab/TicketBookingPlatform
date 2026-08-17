using Domain.Events;
using Domain.Orders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Orders;

internal static class OrderMapper
{
    public static OrderDto ToDto(Order order, IReadOnlyDictionary<Guid, TicketCategory> categoriesById)
    {
        List<OrderItemDto> items = order.Items
            .Select(i => new OrderItemDto(
                i.Id,
                i.TicketCategoryId,
                categoriesById.TryGetValue(i.TicketCategoryId, out TicketCategory? cat) ? cat.Name : string.Empty,
                i.Quantity,
                i.UnitPrice.Amount,
                i.UnitPrice.Currency,
                i.LineTotal.Amount))
            .ToList();

        return new OrderDto(
            order.Id,
            order.UserId,
            order.EventId,
            order.Status,
            order.TotalAmount.Amount,
            order.TotalAmount.Currency,
            order.CreatedAt,
            order.PaidAt,
            order.CancelledAt,
            items);
    }
}
