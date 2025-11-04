using Bid_Go_Backend.Data.Models.Enums;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class CreatePaymentRequestDTO
    {
        public int TransportRequestId { get; set; }
        public string StripeToken { get; set; } = null!;   // "tok_..."
    }
}
