using eShop.Core.Models;
using eShop.Core.Services.Abstractions;
using eShopApi.Core.Enums;

namespace eShop.Core.Services.Implementations
{
    public class PaymentsService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PaymentsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Payment> CreatePaymentAsync(int orderId, int paymentMethodId, decimal amount, string gateway, string? notes = null)
        {
            var payment = new Payment
            {
                OrderId = orderId,
                PaymentMethodId = paymentMethodId,
                Amount = amount,
                Gateway = gateway,
                Status = PaymentStatus.Pending,
                Notes = notes,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.PaymentRepository.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();
            return payment;
        }

        public async Task<Payment> ProcessPaymentAsync(int paymentId, PaymentStatus status, string? gatewayTransactionId = null)
        {
            var payment = await _unitOfWork.PaymentRepository.GetByIdAsync(paymentId);

            if (payment == null)
                throw new KeyNotFoundException($"Payment with ID {paymentId} not found.");

            payment.Status = status;
            payment.GatewayTransactionId = gatewayTransactionId;
            payment.ProcessedAt = DateTime.UtcNow;

            _unitOfWork.PaymentRepository.Update(payment);
            await _unitOfWork.SaveChangesAsync();

            return payment;
        }

        public async Task<IEnumerable<Payment>> GetPaymentsByOrderIdAsync(int orderId)
        {
            return await _unitOfWork.PaymentRepository.FindAllAsync(p => p.OrderId == orderId);
        }

        public async Task<Payment?> GetPaymentByIdAsync(int paymentId)
        {
            return await _unitOfWork.PaymentRepository.GetByIdAsync(paymentId);
        }

        public async Task<bool> RefundPaymentAsync(int paymentId, decimal? amount = null)
        {
            var payment = await _unitOfWork.PaymentRepository.GetByIdAsync(paymentId);

            if (payment == null)
                throw new KeyNotFoundException($"Payment with ID {paymentId} not found.");

            // Here you would integrate with a payment gateway's refund API
            // For now, let's just mark it as refunded in our DB
            payment.Status = PaymentStatus.Refunded;
            payment.Notes += $"\nRefund issued on {DateTime.UtcNow} for {(amount ?? payment.Amount):C}";

            _unitOfWork.PaymentRepository.Update(payment);
            await _unitOfWork.SaveChangesAsync();

            return true;
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
    }
}
