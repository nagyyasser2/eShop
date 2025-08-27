using eShop.Core.Services.Implementations;
using Microsoft.AspNetCore.Authorization;
using eShop.Core.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using eShop.Core.DTOs.Orders;
using System.Security.Claims;
using eShop.Core.Enums;
using eShop.Core.DTOs;
using AutoMapper;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController(IOrderService orderService, IEmailSender emailSender, IMapper mapper) : ControllerBase
    {
        private readonly IOrderService _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        private readonly IEmailSender _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        private readonly IMapper _mapper = mapper ?? throw new ArgumentException(nameof(mapper));

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders([FromQuery] int page, [FromQuery] int size = 10)
        {
            var orders = await _orderService.GetAllOrdersAsync(page, size);
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin");

            if(id.ToString() != userId && !isAdmin)
            {
                return Unauthorized();
            }

            var order = await _orderService.GetOrderByIdAsync(id);

            if (order == null)
                return NotFound(new { Message = $"Order with ID {id} not found." });

            return Ok(order);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != id && !User.IsInRole("Admin"))
            {
                return Unauthorized();
            }

            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(orders);
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto order)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var isAdmin = User.IsInRole("Admin");

            if (userId == null || userEmail == null)
            {
                return BadRequest();
            }

            order.UserId = userId;

            var createdOrder = await _orderService.CreateOrderAsync(order);

            var emailContent = OrderEmailTemplate.GenerateOrderConfirmationEmail(createdOrder);

            await _emailSender.SendEmailAsync(userEmail, "Your eShop Order Confirmation", emailContent);

            return CreatedAtAction(nameof(GetOrderById), new { id = createdOrder.Id }, createdOrder);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateOrderStatus(
            int id,
            [FromQuery] ShippingStatus shippingStatus,
            [FromQuery] PaymentStatus paymentStatus)
        {
            var currentOrder = await _orderService.GetOrderByIdAsync(id);

            if (currentOrder == null)
            {
                return NotFound(new { Message = "Order not found!" });
            }

            var originalShippingStatus = currentOrder.ShippingStatus;
            var originalPaymentStatus = currentOrder.PaymentStatus;

            // Update the order
            var updatedOrder = await _orderService.UpdateOrderStatusAsync(id, shippingStatus, paymentStatus);

            var updatedOrderDto = _mapper.Map<OrderDto>(updatedOrder);

            // Check if either status has changed
            bool statusChanged = originalShippingStatus != updatedOrder.ShippingStatus ||
                                 originalPaymentStatus != updatedOrder.PaymentStatus;

            if (statusChanged)
            {
                var userEmail = User.FindFirstValue(ClaimTypes.Email);
                if (!string.IsNullOrEmpty(userEmail))
                {
                    // Generate email content with both statuses
                    var emailContent = OrderEmailTemplate.GenerateOrderStatusUpdateEmail(
                        updatedOrderDto,
                        originalShippingStatus.ToString(),
                        originalPaymentStatus.ToString());

                    // Choose subject based on what changed
                    string subject;
                    if (originalShippingStatus != updatedOrder.ShippingStatus)
                    {
                        subject = updatedOrder.ShippingStatus switch
                        {
                            ShippingStatus.Shipped => $"🚚 Your Order #{updatedOrderDto.OrderNumber} Has Shipped!",
                            ShippingStatus.Delivered => $"📦 Your Order #{updatedOrderDto.OrderNumber} Has Been Delivered!",
                            _ => $"📝 Order #{updatedOrderDto.OrderNumber} Shipping Status Updated"
                        };
                    }
                    else
                    {
                        // Payment status changed
                        subject = $"💳 Payment Status Updated for Order #{updatedOrderDto.OrderNumber}";
                    }

                    await _emailSender.SendEmailAsync(userEmail, subject, emailContent);
                }
            }

            return Ok(updatedOrderDto);
        }

        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id, [FromBody] CancelOrderRequestDto? request = null)
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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] UpdateOrderDto order)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id.ToString() != order.Id)
                return BadRequest(new { Message = "Order ID mismatch." });

            var updatedOrder = await _orderService.UpdateOrderAsync(order);
            return Ok(updatedOrder);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles ="Admin")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var deleted = await _orderService.DeleteOrderAsync(id);
            if (!deleted)
                return NotFound(new { Message = $"Order with ID {id} not found." });

            return NoContent();
        }
    }
}
