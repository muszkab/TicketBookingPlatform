namespace Application.Orders.Queries.GetMyOrders;

public sealed record GetMyOrdersQuery(int Page = 1, int PageSize = GetMyOrdersQuery.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
}
