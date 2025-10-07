using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.DTOs.Payments
{
    public class RefundPaymentDto
    {
        [Required]
        public int PaymentId { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? Amount { get; set; } // If null, refund full amount

        public string? Reason { get; set; }
    }
}
