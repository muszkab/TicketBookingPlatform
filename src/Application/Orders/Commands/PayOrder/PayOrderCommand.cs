using System;

namespace Application.Orders.Commands.PayOrder;

public sealed record PayOrderCommand(Guid OrderId);
