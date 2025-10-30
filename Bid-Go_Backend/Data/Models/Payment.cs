using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossValue { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetValue { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; }

        [Required]
        public EPaymentStatus PaymentStatus { get; set; }

        [Required]
        public EPaymentMethod PaymentMethod { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [Required]
        public DateTime DeadlineToPay { get; set; }

        public string? FailureReason { get; set; }

        [ForeignKey(nameof(Company))]
        public int CompanyId { get; set; }
        public Company? Company { get; set; } = null;

        [ForeignKey(nameof(Driver))]
        public int DriverId { get; set; }
        public Driver? Driver { get; set; } = null;

        [Required]
        [ForeignKey(nameof(TransportRequest))]
        public int TransportRequestId { get; set; }

        public TransportRequest TransportRequest { get; set; } = null!;

        public string? StripePaymentIntentId { get; set; }
        public string? StripePaymentMethodId { get; set; }
    }
}
