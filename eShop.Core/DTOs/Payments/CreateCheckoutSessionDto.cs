using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.DTOs.Payments
{
    public class CreateCheckoutSessionDto
    {
        public int OrderId { get; set; }
        public string CustomerEmail { get; set; }
    }
}
