using System;

namespace Application.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdQuery(Guid OrderId);
