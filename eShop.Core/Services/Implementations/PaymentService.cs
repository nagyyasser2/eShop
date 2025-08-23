using AutoMapper;
using eShop.Core.DTOs;
using eShop.Core.Models;
using eShop.Core.Services.Abstractions;
using eShop.Core.Enums;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using eShop.Core.DTOs.Payments;
using Microsoft.Extensions.Options;
using eShop.Core.Configurations;

namespace eShop.Core.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStripeService _stripeService;
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;
        private readonly ILogger<PaymentService> _logger;
        private readonly IOptions<StripeSettings> _stripeSettings;

        public PaymentService(
            IUnitOfWork unitOfWork,
            IStripeService stripeService,
            IOrderService orderService,
            IMapper mapper,
            ILogger<PaymentService> logger,
            IOptions<StripeSettings> stripeSettings
            )
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _stripeService = stripeService ?? throw new ArgumentNullException(nameof(stripeService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stripeSettings = stripeSettings ?? throw new ArgumentNullException(nameof(stripeSettings));
        }

        public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto paymentDto)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(paymentDto.OrderId);
                if (order == null)
                    throw new KeyNotFoundException($"Order with ID {paymentDto.OrderId} not found.");

                var payment = new Payment
                {
                    TransactionId = GenerateTransactionId(),
                    Amount = paymentDto.Amount,
                    OrderId = paymentDto.OrderId,
                    PaymentMethodId = paymentDto.PaymentMethodId,
                    Status = PaymentStatus.Pending,
                    Notes = paymentDto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.PaymentRepository.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                return _mapper.Map<PaymentDto>(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment for order {OrderId}", paymentDto.OrderId);
                throw;
            }
        }

        public async Task<StripePaymentIntentDto> CreatePaymentIntentAsync(ProcessStripePaymentDto paymentDto)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(paymentDto.OrderId, new[] { "User" });
                if (order == null)
                    throw new KeyNotFoundException($"Order with ID {paymentDto.OrderId} not found.");

                // Create or get Stripe customer
                var customer = await GetOrCreateStripeCustomerAsync(order.User.Email, $"{order.ShippingFirstName} {order.ShippingLastName}");

                // Create metadata
                var metadata = new Dictionary<string, string>
                {
                    ["order_id"] = paymentDto.OrderId.ToString(),
                    ["user_id"] = order.UserId,
                    ["order_number"] = order.OrderNumber
                };

                if (paymentDto.Metadata != null)
                {
                    foreach (var item in paymentDto.Metadata)
                    {
                        metadata[item.Key] = item.Value;
                    }
                }

                // Create PaymentIntent
                var paymentIntent = await _stripeService.CreatePaymentIntentAsync(
                    order.TotalAmount,
                    "usd", // or get from configuration
                    customer.Id,
                    paymentDto.Description ?? $"Payment for Order #{order.OrderNumber}",
                    metadata
                );

                // Create local payment record
                var payment = new Payment
                {
                    TransactionId = GenerateTransactionId(),
                    Amount = order.TotalAmount,
                    OrderId = paymentDto.OrderId,
                    PaymentMethodId = 1, // Default Stripe payment method ID
                    Status = PaymentStatus.Pending,
                    Gateway = "Stripe",
                    GatewayTransactionId = paymentIntent.Id,
                    Notes = "Stripe PaymentIntent created",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.PaymentRepository.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                return new StripePaymentIntentDto
                {
                    ClientSecret = paymentIntent.ClientSecret,
                    PaymentIntentId = paymentIntent.Id,
                    Amount = order.TotalAmount,
                    Currency = "usd",
                    Status = paymentIntent.Status,
                    OrderId = paymentDto.OrderId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe PaymentIntent for order {OrderId}", paymentDto.OrderId);
                throw;
            }
        }

        public async Task<PaymentDto> ConfirmStripePaymentAsync(ConfirmPaymentDto confirmDto)
        {
            await using var transaction = _unitOfWork.BeginTransaction();

            try
            {
                // Get PaymentIntent from Stripe
                var paymentIntent = await _stripeService.GetPaymentIntentAsync(confirmDto.PaymentIntentId);

                // Find local payment record
                var payment = await _unitOfWork.PaymentRepository
                    .FindAsync(p => p.GatewayTransactionId == confirmDto.PaymentIntentId);

                if (payment == null)
                    throw new KeyNotFoundException($"Payment with PaymentIntent ID {confirmDto.PaymentIntentId} not found.");

                // Update payment status based on Stripe status
                payment.Status = paymentIntent.Status switch
                {
                    "succeeded" => PaymentStatus.Completed,
                    "processing" => PaymentStatus.Pending,
                    "requires_payment_method" => PaymentStatus.Failed,
                    "requires_confirmation" => PaymentStatus.Pending,
                    "requires_action" => PaymentStatus.Pending,
                    "canceled" => PaymentStatus.Cancelled,
                    _ => PaymentStatus.Failed
                };

                payment.ProcessedAt = DateTime.UtcNow;
                payment.Notes = $"Stripe status: {paymentIntent.Status}";

                _unitOfWork.PaymentRepository.Update(payment);

                // Update order payment status if payment succeeded
                if (payment.Status == PaymentStatus.Completed)
                {
                    var order = await _unitOfWork.OrderRepository.GetByIdAsync(payment.OrderId);
                    if (order != null)
                    {
                        order.PaymentStatus = PaymentStatus.Completed;
                        order.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.OrderRepository.Update(order);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return _mapper.Map<PaymentDto>(payment);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<PaymentDto> RefundPaymentAsync(RefundPaymentDto refundDto)
        {
            await using var transaction = _unitOfWork.BeginTransaction();

            try
            {
                var payment = await _unitOfWork.PaymentRepository.GetByIdAsync(refundDto.PaymentId);
                if (payment == null)
                    throw new KeyNotFoundException($"Payment with ID {refundDto.PaymentId} not found.");

                if (payment.Status != PaymentStatus.Completed)
                    throw new InvalidOperationException("Can only refund completed payments.");

                if (string.IsNullOrEmpty(payment.GatewayTransactionId))
                    throw new InvalidOperationException("No Stripe PaymentIntent ID found for this payment.");

                // Create refund in Stripe
                var refund = await _stripeService.CreateRefundAsync(
                    payment.GatewayTransactionId,
                    refundDto.Amount,
                    refundDto.Reason ?? "requested_by_customer"
                );

                // Update payment status
                payment.Status = PaymentStatus.Refunded;
                payment.Notes = $"{payment.Notes}\nRefunded: {refund.Id} - {refundDto.Reason}";
                _unitOfWork.PaymentRepository.Update(payment);

                // Update order status
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(payment.OrderId);
                if (order != null)
                {
                    order.PaymentStatus = PaymentStatus.Refunded;
                    order.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.OrderRepository.Update(order);
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return _mapper.Map<PaymentDto>(payment);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> HandleStripeWebhookAsync(string payload, string signature)
        {
            try
            {
                var webhookSecret = "your_webhook_secret"; // Get from configuration
                var stripeEvent = await _stripeService.ConstructWebhookEventAsync(payload, signature, webhookSecret);

                switch (stripeEvent.Type)
                {
                    case "payment_intent.succeeded":
                        await HandlePaymentIntentSucceeded(stripeEvent);
                        break;
                    case "payment_intent.payment_failed":
                        await HandlePaymentIntentFailed(stripeEvent);
                        break;
                    case "charge.dispute.created":
                        await HandleChargeDispute(stripeEvent);
                        break;
                    default:
                        _logger.LogInformation("Unhandled event type: {EventType}", stripeEvent.Type);
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Stripe webhook");
                return false;
            }
        }

        public async Task<PaymentDto?> GetPaymentByIdAsync(int paymentId)
        {
            var payment = await _unitOfWork.PaymentRepository.GetByIdAsync(paymentId, new[] { "Order", "PaymentMethod" });
            return payment != null ? _mapper.Map<PaymentDto>(payment) : null;
        }

        public async Task<IEnumerable<PaymentDto>> GetPaymentsByOrderIdAsync(int orderId)
        {
            var payments = await _unitOfWork.PaymentRepository.FindAllAsync(
                p => p.OrderId == orderId,
                new[] { "Order", "PaymentMethod" }
            );
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }

        public async Task<PaymentDto> UpdatePaymentAsync(int paymentId, Payment payment)
        {
            payment.Id = paymentId;
            _unitOfWork.PaymentRepository.Update(payment);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<PaymentDto>(payment);
        }

        public async Task<bool> DeletePaymentAsync(int paymentId)
        {
            var payment = await _unitOfWork.PaymentRepository.GetByIdAsync(paymentId);
            if (payment == null)
                return false;

            await _unitOfWork.PaymentRepository.RemoveByIdAsync(paymentId);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetTotalPaidAmountAsync(int orderId)
        {
            var payments = await _unitOfWork.PaymentRepository.FindAllAsync(
                p => p.OrderId == orderId && p.Status == PaymentStatus.Completed
            );
            return payments?.Sum(p => p.Amount) ?? 0;
        }

        public async Task<bool> IsOrderFullyPaidAsync(int orderId)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
            if (order == null)
                return false;

            var totalPaid = await GetTotalPaidAmountAsync(orderId);
            return totalPaid >= order.TotalAmount;
        }

        public async Task<IEnumerable<PaymentDto>> GetPaymentHistoryAsync(string userId)
        {
            var payments = await _unitOfWork.PaymentRepository.FindAllAsync(
                p => p.Order.UserId == userId,
                new[] { "Order", "PaymentMethod" }
            );
            return _mapper.Map<IEnumerable<PaymentDto>>(payments);
        }

        private async Task<Stripe.Customer> GetOrCreateStripeCustomerAsync(string email, string name)
        {
            try
            {
                // Try to find existing customer by email
                var customerService = new CustomerService();
                var customers = await customerService.ListAsync(new CustomerListOptions
                {
                    Email = email,
                    Limit = 1
                });

                if (customers.Data.Any())
                {
                    return customers.Data.First();
                }

                // Create new customer
                return await _stripeService.CreateCustomerAsync(email, name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating Stripe customer for email {Email}", email);
                throw;
            }
        }

        private async Task HandlePaymentIntentSucceeded(Stripe.Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null) return;

            var payment = await _unitOfWork.PaymentRepository
                .FindAsync(p => p.GatewayTransactionId == paymentIntent.Id);

            if (payment != null)
            {
                payment.Status = PaymentStatus.Completed;
                payment.ProcessedAt = DateTime.UtcNow;
                payment.Notes = $"{payment.Notes}\nWebhook: Payment succeeded";

                _unitOfWork.PaymentRepository.Update(payment);

                // Update order status
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(payment.OrderId);
                if (order != null)
                {
                    order.PaymentStatus = PaymentStatus.Completed;
                    order.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.OrderRepository.Update(order);
                }

                await _unitOfWork.SaveChangesAsync();
            }
        }

        private async Task HandlePaymentIntentFailed(Stripe.Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null) return;

            var payment = await _unitOfWork.PaymentRepository
                .FindAsync(p => p.GatewayTransactionId == paymentIntent.Id);

            if (payment != null)
            {
                payment.Status = PaymentStatus.Failed;
                payment.ProcessedAt = DateTime.UtcNow;
                payment.Notes = $"{payment.Notes}\nWebhook: Payment failed - {paymentIntent.LastPaymentError?.Message}";

                _unitOfWork.PaymentRepository.Update(payment);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private async Task HandleChargeDispute(Stripe.Event stripeEvent)
        {
            var dispute = stripeEvent.Data.Object as Dispute;
            if (dispute == null) return;

            // Handle dispute logic here
            _logger.LogWarning("Charge dispute received for charge {ChargeId}: {Reason}",
                dispute.ChargeId, dispute.Reason);
        }

        private string GenerateTransactionId()
        {
            return $"TXN{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        }

        public async Task<Session> CreateCheckoutSessionAsync(CreateCheckoutSessionDto checkoutDto)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(checkoutDto.OrderId, new[] { "User", "OrderItems" });
                if (order == null)
                    throw new KeyNotFoundException($"Order with ID {checkoutDto.OrderId} not found.");

                // Create or get Stripe customer
                var customer = await GetOrCreateStripeCustomerAsync(order.User.Email, $"{order.ShippingFirstName} {order.ShippingLastName}");

                // Create line items from order items
                var lineItems = order.OrderItems.Select(item => new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = _stripeService.ConvertToStripeAmountLong(item.UnitPrice),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.ProductName,
                            Metadata = new Dictionary<string, string>
                            {
                                ["product_id"] = item.ProductId.ToString(),
                                ["variant_id"] = item.ProductVariantId?.ToString() ?? ""
                            }
                        }
                    },
                    Quantity = item.Quantity
                }).ToList();

                // Create metadata
                var metadata = new Dictionary<string, string>
                {
                    ["order_id"] = checkoutDto.OrderId.ToString(),
                    ["user_id"] = order.UserId,
                    ["order_number"] = order.OrderNumber
                };

                var stripeSettings = _stripeSettings.Value;

                var successUrl = stripeSettings.SuccessUrl;
                var cancelUrl = stripeSettings.CancelUrl;

                // Create Checkout Session
                var session = await _stripeService.CreateCheckoutSessionAsync(
                    customer.Id,
                    lineItems,
                    successUrl,
                    cancelUrl,
                    metadata
                );

                // Create local payment record
                var payment = new Payment
                {
                    TransactionId = GenerateTransactionId(),
                    Amount = order.TotalAmount,
                    OrderId = checkoutDto.OrderId,
                    PaymentMethodId = 1, // Default Stripe payment method ID
                    Status = PaymentStatus.Pending,
                    Gateway = "Stripe",
                    GatewayTransactionId = session.Id,
                    Notes = "Stripe Checkout Session created",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.PaymentRepository.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync();

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe Checkout Session for order {OrderId}", checkoutDto.OrderId);
                throw;
            }
        }
    }
}