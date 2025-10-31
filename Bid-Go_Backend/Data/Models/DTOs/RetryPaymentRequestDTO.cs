using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class RetryPaymentRequestDTO
    {
        public string StripeToken { get; set; } = null!;
    }
}
