using eShop.Core.Models;
using eShopApi.Core.Enums;

namespace eShop.Core.Services.Abstractions
{
    public interface IPaymentService
    {
        /// <summary>
        /// Creates a new payment for an order.
        /// </summary>
        Task<Payment> CreatePaymentAsync(int orderId, int paymentMethodId, decimal amount, string gateway, string? notes = null);

        /// <summary>
        /// Processes a payment (marks it as completed or failed).
        /// </summary>
        Task<Payment> ProcessPaymentAsync(int paymentId, PaymentStatus status, string? gatewayTransactionId = null);

        /// <summary>
        /// Gets all payments for a specific order.
        /// </summary>
        Task<IEnumerable<Payment>> GetPaymentsByOrderIdAsync(int orderId);

        /// <summary>
        /// Gets payment by ID.
        /// </summary>
        Task<Payment?> GetPaymentByIdAsync(int paymentId);

        /// <summary>
        /// Refunds a payment (if supported by gateway).
        /// </summary>
        Task<bool> RefundPaymentAsync(int paymentId, decimal? amount = null);

        /// <summary>
        /// Deletes a payment record.
        /// </summary>
        Task<bool> DeletePaymentAsync(int paymentId);
    }
}
