using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.Models
{
    public class PaymentMethod
    {
        public int Id { get; set; }
        public string Name { get; set; } // "Credit Card", "PayPal", etc.
        public string Type { get; set; } // "stripe", "paypal", etc.
        public bool IsActive { get; set; } = true;
        public string? Configuration { get; set; } // JSON config if needed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
