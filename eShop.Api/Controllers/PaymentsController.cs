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

        [HttpPost("create-payment-intent")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentIntent([FromBody] ProcessStripePaymentDto paymentDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userEmail = User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
                    return Unauthorized();

                // Set customer email if not provided
                if (string.IsNullOrEmpty(paymentDto.CustomerEmail))
                    paymentDto.CustomerEmail = userEmail;

                var paymentIntent = await _paymentService.CreatePaymentIntentAsync(paymentDto);
                return Ok(paymentIntent);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment intent for order {OrderId}", paymentDto.OrderId);
                return StatusCode(500, new { Message = "Failed to create payment intent.", Error = ex.Message });
            }
        }

        [HttpPost("create-checkout-session")]
        [Authorize]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionDto checkoutDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userEmail = User.FindFirstValue(ClaimTypes.Email);

                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userEmail))
                    return Unauthorized();

                checkoutDto.CustomerEmail = userEmail;

                var session = await _paymentService.CreateCheckoutSessionAsync(checkoutDto);
                return Ok(new { SessionId = session.Id, Url = session.Url });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout session for order {OrderId}", checkoutDto.OrderId);
                return StatusCode(500, new { Message = "Failed to create checkout session.", Error = ex.Message });
            }
        }
        
        [HttpPost("confirm")]
        [Authorize]
        public async Task<IActionResult> ConfirmPayment([FromBody] ConfirmPaymentDto confirmDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var payment = await _paymentService.ConfirmStripePaymentAsync(confirmDto);
                return Ok(new { Message = "Payment confirmed successfully.", Payment = payment });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment for PaymentIntent {PaymentIntentId}",
                    confirmDto.PaymentIntentId);
                return StatusCode(500, new { Message = "Failed to confirm payment.", Error = ex.Message });
            }
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

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            try
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
            catch (UnauthorizedAccessException)
            {
                return Unauthorized("Invalid webhook signature.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Stripe webhook");
                return StatusCode(500, "Webhook processing failed.");
            }
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