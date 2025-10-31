using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class PaymentResultDTO
    {
        public int PaymentId { get; set; }
        public decimal GrossValue { get; set; }
        public decimal Tax { get; set; }
        public decimal NetValue { get; set; }
        public EPaymentStatus Status { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
