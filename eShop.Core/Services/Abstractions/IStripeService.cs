using Stripe;
using Stripe.Checkout;

namespace eShop.Core.Services.Abstractions
{
    public interface IStripeService
    {
        Task<Session> CreateCheckoutSessionAsync(string customerId, List<SessionLineItemOptions> lineItems, string successUrl, string cancelUrl, Dictionary<string, string>? metadata = null);
        Task<Customer> CreateCustomerAsync(string email, string? name = null, Dictionary<string, string>? metadata = null);
        Task<Customer> GetCustomerAsync(string customerId);
        Task<Customer> UpdateCustomerAsync(string customerId, string? email = null, string? name = null);
        Task<PaymentMethod> GetPaymentMethodAsync(string paymentMethodId);
        Task<PaymentMethod> AttachPaymentMethodToCustomerAsync(string paymentMethodId, string customerId);
        Task<PaymentMethod> DetachPaymentMethodAsync(string paymentMethodId);
        Task<Refund> CreateRefundAsync(string paymentIntentId, decimal? amount = null, string? reason = null);
        Task<Refund> GetRefundAsync(string refundId);
        Task<Stripe.Event> ConstructWebhookEventAsync(string payload, string signature, string endpointSecret);
        Task<bool> ValidateWebhookSignatureAsync(string payload, string signature, string endpointSecret);
        string ConvertToStripeAmount(decimal amount, string currency = "usd");
        decimal ConvertFromStripeAmount(long amount, string currency = "usd");
        long ConvertToStripeAmountLong(decimal amount, string currency = "usd");

    }
}