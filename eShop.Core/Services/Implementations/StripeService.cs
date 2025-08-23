using Stripe;
using eShop.Core.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe.Checkout;

namespace eShop.Core.Services.Implementations
{
    public class StripeService : IStripeService
    {
        private readonly PaymentIntentService _paymentIntentService;
        private readonly CustomerService _customerService;
        private readonly PaymentMethodService _paymentMethodService;
        private readonly RefundService _refundService;
        private readonly SessionService _sessionService;
        private readonly ILogger<StripeService> _logger;
        private readonly string _webhookSecret;

        public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
        {
            var secretKey = configuration["Stripe:SecretKey"];
            _webhookSecret = configuration["Stripe:WebhookSecret"];

            StripeConfiguration.ApiKey = secretKey;

            _paymentIntentService = new PaymentIntentService();
            _customerService = new CustomerService();
            _paymentMethodService = new PaymentMethodService();
            _refundService = new RefundService();
            _sessionService = new SessionService();
            _logger = logger;
        }

        public async Task<PaymentIntent> CreatePaymentIntentAsync(decimal amount, string currency,
            string customerId, string description, Dictionary<string, string>? metadata = null)
        {
            try
            {
                var options = new PaymentIntentCreateOptions
                {
                    Amount = ConvertToStripeAmountLong(amount, currency),
                    Currency = currency.ToLower(),
                    Customer = customerId,
                    Description = description,
                    Metadata = metadata,
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true
                    }
                };

                return await _paymentIntentService.CreateAsync(options);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error creating PaymentIntent for amount {Amount}", amount);
                throw new InvalidOperationException($"Failed to create payment intent: {ex.Message}", ex);
            }
        }

        public async Task<PaymentIntent> ConfirmPaymentIntentAsync(string paymentIntentId, string paymentMethodId)
        {
            try
            {
                var options = new PaymentIntentConfirmOptions
                {
                    PaymentMethod = paymentMethodId
                };

                return await _paymentIntentService.ConfirmAsync(paymentIntentId, options);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error confirming PaymentIntent {PaymentIntentId}", paymentIntentId);
                throw new InvalidOperationException($"Failed to confirm payment: {ex.Message}", ex);
            }
        }

        public async Task<PaymentIntent> GetPaymentIntentAsync(string paymentIntentId)
        {
            try
            {
                return await _paymentIntentService.GetAsync(paymentIntentId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error retrieving PaymentIntent {PaymentIntentId}", paymentIntentId);
                throw new KeyNotFoundException($"Payment intent not found: {ex.Message}", ex);
            }
        }

        public async Task<PaymentIntent> CancelPaymentIntentAsync(string paymentIntentId)
        {
            try
            {
                return await _paymentIntentService.CancelAsync(paymentIntentId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error canceling PaymentIntent {PaymentIntentId}", paymentIntentId);
                throw new InvalidOperationException($"Failed to cancel payment: {ex.Message}", ex);
            }
        }

        public async Task<Customer> CreateCustomerAsync(string email, string? name = null,
            Dictionary<string, string>? metadata = null)
        {
            try
            {
                var options = new CustomerCreateOptions
                {
                    Email = email,
                    Name = name,
                    Metadata = metadata
                };

                return await _customerService.CreateAsync(options);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error creating customer for email {Email}", email);
                throw new InvalidOperationException($"Failed to create customer: {ex.Message}", ex);
            }
        }

        public async Task<Customer> GetCustomerAsync(string customerId)
        {
            try
            {
                return await _customerService.GetAsync(customerId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error retrieving customer {CustomerId}", customerId);
                throw new KeyNotFoundException($"Customer not found: {ex.Message}", ex);
            }
        }

        public async Task<Customer> UpdateCustomerAsync(string customerId, string? email = null, string? name = null)
        {
            try
            {
                var options = new CustomerUpdateOptions();

                if (!string.IsNullOrEmpty(email))
                    options.Email = email;

                if (!string.IsNullOrEmpty(name))
                    options.Name = name;

                return await _customerService.UpdateAsync(customerId, options);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error updating customer {CustomerId}", customerId);
                throw new InvalidOperationException($"Failed to update customer: {ex.Message}", ex);
            }
        }

        public async Task<PaymentMethod> GetPaymentMethodAsync(string paymentMethodId)
        {
            try
            {
                return await _paymentMethodService.GetAsync(paymentMethodId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error retrieving PaymentMethod {PaymentMethodId}", paymentMethodId);
                throw new KeyNotFoundException($"Payment method not found: {ex.Message}", ex);
            }
        }

        public async Task<PaymentMethod> AttachPaymentMethodToCustomerAsync(string paymentMethodId, string customerId)
        {
            try
            {
                var options = new PaymentMethodAttachOptions
                {
                    Customer = customerId
                };

                return await _paymentMethodService.AttachAsync(paymentMethodId, options);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error attaching PaymentMethod {PaymentMethodId} to customer {CustomerId}",
                    paymentMethodId, customerId);
                throw new InvalidOperationException($"Failed to attach payment method: {ex.Message}", ex);
            }
        }

        public async Task<PaymentMethod> DetachPaymentMethodAsync(string paymentMethodId)
        {
            try
            {
                return await _paymentMethodService.DetachAsync(paymentMethodId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error detaching PaymentMethod {PaymentMethodId}", paymentMethodId);
                throw new InvalidOperationException($"Failed to detach payment method: {ex.Message}", ex);
            }
        }

        public async Task<Refund> CreateRefundAsync(string paymentIntentId, decimal? amount = null, string? reason = null)
        {
            try
            {
                var options = new RefundCreateOptions
                {
                    PaymentIntent = paymentIntentId,
                    Reason = reason switch
                    {
                        "duplicate" => "duplicate",
                        "fraudulent" => "fraudulent",
                        "requested_by_customer" => "requested_by_customer",
                        _ => "requested_by_customer"
                    }
                };

                if (amount.HasValue)
                {
                    options.Amount = ConvertToStripeAmountLong(amount.Value);
                }

                return await _refundService.CreateAsync(options);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error creating refund for PaymentIntent {PaymentIntentId}", paymentIntentId);
                throw new InvalidOperationException($"Failed to create refund: {ex.Message}", ex);
            }
        }

        public async Task<Refund> GetRefundAsync(string refundId)
        {
            try
            {
                return await _refundService.GetAsync(refundId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error retrieving refund {RefundId}", refundId);
                throw new KeyNotFoundException($"Refund not found: {ex.Message}", ex);
            }
        }

        public async Task<Stripe.Event> ConstructWebhookEventAsync(string payload, string signature, string endpointSecret)
        {
            try
            {
                return await Task.FromResult(EventUtility.ConstructEvent(payload, signature, endpointSecret));
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error constructing webhook event");
                throw new UnauthorizedAccessException($"Invalid webhook signature: {ex.Message}", ex);
            }
        }

        public async Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, string endpointSecret)
        {
            try
            {
                EventUtility.ConstructEvent(payload, signature, endpointSecret);
                return await Task.FromResult(true);
            }
            catch (StripeException)
            {
                return await Task.FromResult(false);
            }
        }

        public string ConvertToStripeAmount(decimal amount, string currency = "usd")
        {
            return ConvertToStripeAmountLong(amount, currency).ToString();
        }

        public decimal ConvertFromStripeAmount(long amount, string currency = "usd")
        {
            // Most currencies use cents (divide by 100), but some don't have fractional units
            var zeroDecimalCurrencies = new[] { "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg", "rwf", "ugx", "vnd", "vuv", "xaf", "xof", "xpf" };

            if (zeroDecimalCurrencies.Contains(currency.ToLower()))
            {
                return amount;
            }

            return amount / 100m;
        }

        public long ConvertToStripeAmountLong(decimal amount, string currency = "usd")
        {
            // Most currencies use cents (multiply by 100), but some don't have fractional units
            var zeroDecimalCurrencies = new[] { "bif", "clp", "djf", "gnf", "jpy", "kmf", "krw", "mga", "pyg", "rwf", "ugx", "vnd", "vuv", "xaf", "xof", "xpf" };

            if (zeroDecimalCurrencies.Contains(currency.ToLower()))
            {
                return (long)amount;
            }

            return (long)(amount * 100);
        }

        public async Task<Session> CreateCheckoutSessionAsync(string customerId, List<SessionLineItemOptions> lineItems, string successUrl, string cancelUrl, Dictionary<string, string>? metadata = null)
        {
            try
            {
                var options = new SessionCreateOptions
                {
                    Customer = customerId,
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = lineItems,
                    Mode = "payment",
                    SuccessUrl = successUrl,
                    CancelUrl = cancelUrl,
                    Metadata = metadata,
                    AutomaticTax = new SessionAutomaticTaxOptions
                    {
                        Enabled = false
                    }
                };

                return await _sessionService.CreateAsync(options);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Error creating Checkout Session for customer {CustomerId}", customerId);
                throw new InvalidOperationException($"Failed to create checkout session: {ex.Message}", ex);
            }
        }
    }
}