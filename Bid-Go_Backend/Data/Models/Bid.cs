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
    public class Bid
    {
        [Key]
        public int BidId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }

        [Required]
        public DateTime DeliveryDeadline { get; set; }

        [Required]
        public EBidStatus Status { get; set; }

        [ForeignKey(nameof(Driver))]
        public int DriverId { get; set; }
        public Driver? Driver { get; set; } = null;



        [ForeignKey(nameof(TransportRequest))]
        public int TransportRequestId { get; set; }
        public TransportRequest TransportRequest { get; set; } = null!;
    }
}
