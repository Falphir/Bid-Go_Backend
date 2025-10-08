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
        public float GrossValue { get; set; }

        [Required]
        public float NetValue { get; set; }

        [Required]
        public float Tax { get; set; }

        [Required]
        public EPaymentStatus PaymentStatus { get; set; }

        [Required]
        public EPaymentMethod PaymentMethod { get; set; }

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

    }
}
