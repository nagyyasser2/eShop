using eShopApi.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;


namespace eShop.Core.Models
{
    public class Payment
    {
        public int Id { get; set; }
        [Required, MaxLength(100)]
        public string TransactionId { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public string? Gateway { get; set; }
        public string? GatewayTransactionId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }

        // Foreign Keys
        public int OrderId { get; set; }
        public int PaymentMethodId { get; set; }

        // Navigation Properties
        public virtual Order Order { get; set; }
        public virtual PaymentMethod PaymentMethod { get; set; }
    }
}
