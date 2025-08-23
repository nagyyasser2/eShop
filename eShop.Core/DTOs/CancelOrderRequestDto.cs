using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eShop.Core.DTOs
{
    public class CancelOrderRequestDto
    {
        [MaxLength(500)]
        public string? Reason { get; set; }
    }
}
