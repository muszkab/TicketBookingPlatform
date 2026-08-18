using Domain.Common;
using Domain.Events;
using Domain.Orders;
using System;

namespace Domain.Tests.Common.Builders;

internal sealed class OrderBuilder
{
    private Guid _userId = Guid.NewGuid();
    private Guid _eventId = Guid.NewGuid();
    private string _currency = "EUR";

    public OrderBuilder ForEvent(Guid eventId) { _eventId = eventId; return this; }
    public OrderBuilder ForUser(Guid userId) { _userId = userId; return this; }
    public OrderBuilder WithCurrency(string currency) { _currency = currency; return this; }

    public Order Build() => new(_userId, _eventId, _currency);

    public static TicketCategory NewCategory(Guid eventId, string name = "Std", decimal price = 10m, int quantity = 5)
        => new(eventId, name, new Money(price, "EUR"), quantity);
}
