using Microsoft.AspNetCore.Authorization;
using eShop.Core.Services.Abstractions;
using eShop.Core.DTOs.Payments;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using eShop.Core.DTOs;

namespace eShop.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger) : ControllerBase
    {
        private readonly ILogger<PaymentsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IPaymentService _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentById(int id)
        {
            try
            {
                var payment = await _paymentService.GetPaymentByIdAsync(id);
                if (payment == null)
                    return NotFound(new { Message = $"Payment with ID {id} not found." });

                // Check if user owns this payment or is admin
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (payment.Order?.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid("You can only view your own payments.");

                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment {PaymentId}", id);
                return StatusCode(500, new { Message = "Failed to retrieve payment." });
            }
        }

        [HttpGet("order/{orderId}")]
        [Authorize]
        public async Task<IActionResult> GetPaymentsByOrderId(int orderId)
        {
            try
            {
                var payments = await _paymentService.GetPaymentsByOrderIdAsync(orderId);

                // Check ownership
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var firstPayment = payments.FirstOrDefault();
                if (firstPayment?.Order?.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid("You can only view payments for your own orders.");

                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payments for order {OrderId}", orderId);
                return StatusCode(500, new { Message = "Failed to retrieve payments." });
            }
        }

        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetPaymentHistory()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var payments = await _paymentService.GetPaymentHistoryAsync(userId);
                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment history for user {UserId}",
                    User.FindFirstValue(ClaimTypes.NameIdentifier));
                return StatusCode(500, new { Message = "Failed to retrieve payment history." });
            }
        }

        [HttpPost("create-checkout-session")]
        [Authorize]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionDto checkoutDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
                return Unauthorized();

            checkoutDto.CustomerEmail = userEmail;

            var session = await _paymentService.CreateCheckoutSessionAsync(checkoutDto);
            return Ok(new { SessionId = session.Id, session.Url });
           
        }
        
        [HttpPost("refund")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RefundPayment([FromBody] RefundPaymentDto refundDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var payment = await _paymentService.RefundPaymentAsync(refundDto);
                return Ok(new { Message = "Payment refunded successfully.", Payment = payment });
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
                _logger.LogError(ex, "Error refunding payment {PaymentId}", refundDto.PaymentId);
                return StatusCode(500, new { Message = "Failed to refund payment.", Error = ex.Message });
            }
        }

        [HttpPost("stripe-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            var payload = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

            if (string.IsNullOrEmpty(signature))
                return BadRequest("Missing Stripe signature.");

            var success = await _paymentService.HandleStripeWebhookAsync(payload, signature);

            if (success)
                return Ok();
            else
                return BadRequest("Failed to process webhook.");
        }

        [HttpGet("order/{orderId}/total-paid")]
        [Authorize]
        public async Task<IActionResult> GetTotalPaidAmount(int orderId)
        {
            try
            {
                var totalPaid = await _paymentService.GetTotalPaidAmountAsync(orderId);
                var isFullyPaid = await _paymentService.IsOrderFullyPaidAsync(orderId);

                return Ok(new
                {
                    OrderId = orderId,
                    TotalPaid = totalPaid,
                    IsFullyPaid = isFullyPaid
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving payment total for order {OrderId}", orderId);
                return StatusCode(500, new { Message = "Failed to retrieve payment total." });
            }
        }
    }
}