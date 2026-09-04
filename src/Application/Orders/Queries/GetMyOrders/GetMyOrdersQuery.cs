namespace Application.Orders.Queries.GetMyOrders;

public sealed record GetMyOrdersQuery(
    int Page = 1,
    int PageSize = GetMyOrdersQuery.DefaultPageSize,
    OrderSortField SortBy = OrderSortField.CreatedAt,
    SortDirection SortDir = SortDirection.Desc)
{
    public const int DefaultPageSize = 20;
}

public enum OrderSortField
{
    CreatedAt,
    TotalAmount,
    Status,
    EventTitle
}

public enum SortDirection
{
    Asc,
    Desc
}
