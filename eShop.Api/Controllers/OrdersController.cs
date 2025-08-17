using eShop.Core.DTOs;
using eShop.Core.Models;
using eShop.Core.Services.Abstractions;
using eShop.Core.Services.Implementations;
using eShopApi.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(IOrderService orderService, IEmailSender emailSender) : ControllerBase
    {
        private readonly IOrderService _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        private readonly IEmailSender _emailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        // GET: api/orders
        [HttpGet]
        public async Task<IActionResult> GetAllOrders([FromQuery] bool includeItems = false, [FromQuery] bool includePayments = false)
        {
            var orders = await _orderService.GetAllOrdersAsync(includeItems, includePayments);
            return Ok(orders);
        }

        // GET: api/orders/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id, [FromQuery] bool includeItems = false, [FromQuery] bool includePayments = false)
        {
            var order = await _orderService.GetOrderByIdAsync(id, includeItems, includePayments);
            if (order == null)
                return NotFound(new { Message = $"Order with ID {id} not found." });

            return Ok(order);
        }

        // GET: api/orders/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(string userId, [FromQuery] bool includeItems = false, [FromQuery] bool includePayments = false)
        {
            var orders = await _orderService.GetOrdersByUserIdAsync(userId, includeItems, includePayments);
            return Ok(orders);
        }

        // POST: api/orders
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

        // PUT: api/orders/{id}/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromQuery] OrderStatus status)
        {
            try
            {
                var updatedOrder = await _orderService.UpdateOrderStatusAsync(id, status);
                return Ok(updatedOrder);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { ex.Message });
            }
        }

        // PUT: api/orders/{id}
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

        // DELETE: api/orders/{id}
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
