using eShopApi.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.DTOs
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
        public string? Gateway { get; set; }
        public string? GatewayTransactionId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int PaymentMethodId { get; set; }
    }
    public class CreatePaymentDto
    {
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string? Gateway { get; set; }
        public string? GatewayTransactionId { get; set; }
        public string? Notes { get; set; }
        public int PaymentMethodId { get; set; }
    }
}
