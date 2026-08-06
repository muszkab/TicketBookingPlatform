namespace WebApi.Contracts.Events;

public sealed record AddTicketCategoryRequest(
    string Name,
    decimal Price,
    string Currency,
    int Quantity);
