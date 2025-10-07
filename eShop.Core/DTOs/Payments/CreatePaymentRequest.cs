using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.DTOs.Payments
{
    public class CreatePaymentRequest
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
}
