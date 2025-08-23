using eShop.Core.Enums;
using System.ComponentModel.DataAnnotations;

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
        public int OrderId { get; set; }
        public int PaymentMethodId { get; set; }

        // Navigation properties
        public OrderDto? Order { get; set; }
        public PaymentMethodDto? PaymentMethod { get; set; }
    }

    public class CreatePaymentDto
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public int PaymentMethodId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        public string? Notes { get; set; }
    }


    public class ProcessStripePaymentDto
    {
        [Required(ErrorMessage = "Order ID is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Order ID must be a positive integer.")]
        public int OrderId { get; set; }

        [Required]
        public string PaymentMethodId { get; set; }

        public string? CustomerEmail { get; set; }

        public bool SavePaymentMethod { get; set; } = false;

        public string? Description { get; set; }

        public Dictionary<string, string>? Metadata { get; set; }
    }

    public class StripePaymentIntentDto
    {
        public string ClientSecret { get; set; }
        public string PaymentIntentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public int OrderId { get; set; }
    }

    public class ConfirmPaymentDto
    {
        [Required]
        public string PaymentIntentId { get; set; }

        [Required]
        public int OrderId { get; set; }
    }

    public class RefundPaymentDto
    {
        [Required]
        public int PaymentId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Amount { get; set; } // If null, refund full amount

        public string? Reason { get; set; }
    }

    public class PaymentMethodDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsActive { get; set; }
        public string? Configuration { get; set; }
    }

    public class WebhookEventDto
    {
        public string EventType { get; set; }
        public string PaymentIntentId { get; set; }
        public string Status { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }
}