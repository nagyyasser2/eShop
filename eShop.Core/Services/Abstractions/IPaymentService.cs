using eShop.Core.DTOs;
using eShop.Core.DTOs.Payments;
using eShop.Core.Models;
using Stripe;
using Stripe.Checkout;

namespace eShop.Core.Services.Abstractions
{
    public interface IPaymentService
    {
        Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto paymentDto);
        Task<Session> CreateCheckoutSessionAsync(CreateCheckoutSessionDto checkoutDto);
        Task<PaymentDto> RefundPaymentAsync(RefundPaymentDto refundDto);
        Task<bool> HandleStripeWebhookAsync(string payload, string signature);
        Task<PaymentDto?> GetPaymentByIdAsync(int paymentId);
        Task<IEnumerable<PaymentDto>> GetPaymentsByOrderIdAsync(int orderId);
        Task<PaymentDto> UpdatePaymentAsync(int paymentId, Payment payment);
        Task<bool> DeletePaymentAsync(int paymentId);
        Task<decimal> GetTotalPaidAmountAsync(int orderId);
        Task<bool> IsOrderFullyPaidAsync(int orderId);
        Task<IEnumerable<PaymentDto>> GetPaymentHistoryAsync(string userId);
    }
}