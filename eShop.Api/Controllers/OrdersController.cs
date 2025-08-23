using AutoMapper;
using eShop.Core.DTOs;
using eShop.Core.Models;
using eShop.Core.Services.Abstractions;
using eShop.Core.Services.Implementations;
using eShop.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(IOrderService orderService, IEmailSender emailSender, IMapper mapper) : ControllerBase
    {
        private readonly IOrderService _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        private readonly IEmailSender _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentException(nameof(mapper));
        
        [HttpGet]
        public async Task<IActionResult> GetAllOrders([FromQuery] bool includeItems = false, [FromQuery] bool includePayments = false)
        {
            var orders = await _orderService.GetAllOrdersAsync(includeItems, includePayments);
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id, [FromQuery] bool includeItems = false, [FromQuery] bool includePayments = false)
        {
            var order = await _orderService.GetOrderByIdAsync(id, includeItems, includePayments);
            if (order == null)
                return NotFound(new { Message = $"Order with ID {id} not found." });

            return Ok(order);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(string userId, [FromQuery] bool includeItems = false, [FromQuery] bool includePayments = false)
        {
            var orders = await _orderService.GetOrdersByUserIdAsync(userId, includeItems, includePayments);
            return Ok(orders);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto order)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userEmail = User.FindFirstValue(ClaimTypes.Email);

                if (userId == null || userEmail == null)
                {
                    return BadRequest();
                }

                var createdOrder = await _orderService.CreateOrderAsync(order, userId);

                var emailContent = OrderEmailTemplate.GenerateOrderConfirmationEmail(createdOrder);

                await _emailSender.SendEmailAsync(userEmail, "Your eShop Order Confirmation", emailContent);

                return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, createdOrder);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to create order.", Error = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateOrderStatus(
            int id,
            [FromQuery] ShippingStatus status,
            [FromQuery] DateTime? shippedAt = null,
            [FromQuery] DateTime? deliveredAt = null)
        {
            try
            {
                var currentOrder = await _orderService.GetOrderByIdAsync(id);
                if (currentOrder == null)
                {
                    return NotFound(new { Message = "Order not found." });
                }
                
                var originalStatus = currentOrder.ShippingStatus;

                var updatedOrder = await _orderService.UpdateOrderStatusAsync(id, status);
                var updatedOrderDto = _mapper.Map<OrderDto>(updatedOrder);

                if (status == ShippingStatus.Shipped || status == ShippingStatus.Delivered)
                {
                    var additionalUpdateDto = new UpdateOrderDto
                    {
                        ShippedAt = status == ShippingStatus.Shipped ? shippedAt ?? DateTime.UtcNow : null,
                        DeliveredAt = status == ShippingStatus.Delivered ? deliveredAt ?? DateTime.UtcNow : null
                    };
                }

                if (originalStatus != updatedOrder.ShippingStatus)
                {
                    var userEmail = User.FindFirstValue(ClaimTypes.Email);
                    if (!string.IsNullOrEmpty(userEmail))
                    {
                        var emailContent = OrderEmailTemplate.GenerateOrderStatusUpdateEmail(
                            updatedOrderDto,
                            originalStatus.ToString());

                        var subject = status switch
                        {
                            ShippingStatus.Shipped => $"Your Order #{updatedOrderDto.OrderNumber} Has Shipped!",
                            ShippingStatus.Delivered => $"Your Order #{updatedOrderDto.OrderNumber} Has Been Delivered!",
                            _ => $"Order #{updatedOrderDto.OrderNumber} Status Update: {updatedOrderDto.ShippingStatus}"
                        };

                        await _emailSender.SendEmailAsync(userEmail, subject, emailContent);
                    }
                }

                return Ok(updatedOrderDto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Failed to update order status.",
                    Error = ex.Message,
                    Details = ex.InnerException?.Message
                });
            }
        }

        [HttpPatch("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequestDto? request = null)
        {
            try
            {
                var currentOrder = await _orderService.GetOrderByIdAsync(id);
                if (currentOrder == null)
                {
                    return NotFound(new { Message = "Order not found." });
                }

                // Optional: Check if the user owns this order (for user-level authorization)
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (currentOrder.UserId != userId && !User.IsInRole("Admin"))
                {
                    return Forbid("You can only cancel your own orders.");
                }

                var cancelledOrder = await _orderService.CancelOrderAsync(id, request?.Reason);

                // Send cancellation email
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (!string.IsNullOrEmpty(userEmail))
                {
                    var emailContent = OrderEmailTemplate.GenerateOrderCancellationEmail(cancelledOrder, request?.Reason);
                    var subject = $"Order #{cancelledOrder.OrderNumber} Cancellation Confirmation";
                    await _emailSender.SendEmailAsync(userEmail, subject, emailContent);
                }

                return Ok(new
                {
                    Message = "Order cancelled successfully.",
                    Order = cancelledOrder
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Failed to cancel order.",
                    Error = ex.Message,
                    Details = ex.InnerException?.Message
                });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] Order order)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != order.Id)
                return BadRequest(new { Message = "Order ID mismatch." });

            var updatedOrder = await _orderService.UpdateOrderAsync(order);
            return Ok(updatedOrder);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var deleted = await _orderService.DeleteOrderAsync(id);
            if (!deleted)
                return NotFound(new { Message = $"Order with ID {id} not found." });

            return NoContent();
        }
    }
}
