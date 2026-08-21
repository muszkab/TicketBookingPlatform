using Application.Common.Paging;
using Application.Orders;
using Application.Orders.Commands.CancelOrder;
using Application.Orders.Commands.CreateOrder;
using Application.Orders.Commands.PayOrder;
using Application.Orders.Queries.GetMyOrders;
using Application.Orders.Queries.GetOrderById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Contracts.Orders;

namespace WebApi.Controllers;

[Authorize]
public class OrdersController(
    CreateOrderCommandHandler createOrderCommandHandler,
    PayOrderCommandHandler payOrderCommandHandler,
    CancelOrderCommandHandler cancelOrderCommandHandler,
    GetOrderByIdQueryHandler getOrderByIdQueryHandler,
    GetMyOrdersQueryHandler getMyOrdersQueryHandler)
    : ApiControllerBase
{
    [HttpPost(Name = nameof(CreateOrder))]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            request.EventId,
            request.Currency,
            request.Items?
                .Select(i => new CreateOrderItemDto(i.TicketCategoryId, i.Quantity))
                .ToList() ?? []);

        OrderDto created = await createOrderCommandHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetOrderById), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/pay", Name = nameof(PayOrder))]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> PayOrder(Guid id, CancellationToken cancellationToken)
    {
        OrderDto result = await payOrderCommandHandler.HandleAsync(new PayOrderCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/cancel", Name = nameof(CancelOrder))]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderDto>> CancelOrder(Guid id, CancellationToken cancellationToken)
    {
        OrderDto result = await cancelOrderCommandHandler.HandleAsync(new CancelOrderCommand(id), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}", Name = nameof(GetOrderById))]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderDto>> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        OrderDto? order = await getOrderByIdQueryHandler.HandleAsync(new GetOrderByIdQuery(id), cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("mine", Name = nameof(GetMyOrders))]
    [ProducesResponseType(typeof(PagedResult<OrderSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> GetMyOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = GetMyOrdersQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        PagedResult<OrderSummaryDto> result = await getMyOrdersQueryHandler.HandleAsync(new GetMyOrdersQuery(page, pageSize), cancellationToken);
        return Ok(result);
    }
}
