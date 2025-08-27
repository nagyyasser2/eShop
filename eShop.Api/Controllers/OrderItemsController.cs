using Microsoft.AspNetCore.Authorization;
using eShop.Core.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using eShop.Core.DTOs.Orders;
using eShop.Core.Exceptions;
using AutoMapper;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrderItemsController(IOrderItemService orderItemService, IOrderService orderService, IMapper mapper) : ControllerBase
    {
        private readonly IOrderItemService _orderItemService = orderItemService ?? throw new ArgumentNullException(nameof(orderItemService));
        private readonly IOrderService _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<OrderItemDto>> CreateOrderItem([FromBody] CreateOrderItemDto orderItemDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) { 
                throw new ArgumentNullException(nameof(userId));
            }

            var isAdmin = User.IsInRole("Admin");

            var order = await _orderService.GetOrderByIdAsync(orderItemDto.OrderId);

            if (order == null) {
                throw new NotFoundException($"order with id: {orderItemDto.OrderId} not found!.");
            }
           
            if(order.UserId != userId && !isAdmin)
            {
                throw new ForbiddenException("Forbidden!");
            }

            var createdOrderItem = await _orderItemService.CreateOrderItemAsync(orderItemDto);

            var resultDto = _mapper.Map<OrderItemDto>(createdOrderItem);

            return CreatedAtAction(nameof(GetOrderItemById), new { id = resultDto.Id }, resultDto);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderItemDto>> GetOrderItemById(int id)
        {
            var user = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var order = await _orderService.GetOrderByIdAsync(id);

            if (order == null) 
                return NotFound($"Order with id:{id} , NotFound!.");
            

            if (order.UserId != user && !isAdmin)  
                return Forbid();
           

            var orderItem = await _orderItemService.GetOrderItemByIdAsync(id);

            if (orderItem == null)
                return NotFound($"Order item with ID {id} not found.");

            var orderItemDto = _mapper.Map<OrderItemDto>(orderItem);
            return Ok(orderItemDto);
        }

        [HttpGet("order/{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<OrderItemDto>>> GetOrderItemsByOrderId(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
                return NotFound();
            if (order.UserId != userId && !isAdmin) {  return Forbid(); }

            var orderItems = await _orderItemService.GetOrderItemsByOrderIdAsync(orderId);
            if (!orderItems.Any())
                return NotFound($"No order items found for order ID {orderId}.");

            var orderItemDtos = _mapper.Map<IEnumerable<OrderItemDto>>(orderItems);
            return Ok(orderItemDtos);
        }

        [HttpPut()]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<OrderItemDto>> UpdateOrderItemQuantity([FromBody] UpdateOrderItemDto updateOrderItemDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var orderItem = await _orderItemService.GetOrderItemByIdAsync(updateOrderItemDto.Id);
            if (orderItem == null)
                return NotFound($"Order item with ID {updateOrderItemDto.Id} not found.");

            var order = await _orderService.GetOrderByIdAsync(orderItem.OrderId);
            if (order == null) return NotFound();

            if(order.UserId != userId.ToString() && !isAdmin) { return Forbid(); }   

            var updatedOrderItem = await _orderItemService.UpdateOrderItemQuantityAsync(updateOrderItemDto);

            var resultDto = _mapper.Map<OrderItemDto>(updatedOrderItem);
            return Ok(resultDto);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteOrderItem(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            var orderItem = await _orderItemService.GetOrderItemByIdAsync(id);
            if (orderItem == null)
                return NotFound();  

            var order = await _orderService.GetOrderByIdAsync(orderItem.OrderId);
            if (order == null)
                return NotFound();

            if (order.UserId != userId && !isAdmin)
                return Forbid();

            var deleted = await _orderItemService.DeleteOrderItemAsync(id);
            if (!deleted)
                return NotFound($"Order item with ID {id} not found.");

            return NoContent();
        }
    }
}