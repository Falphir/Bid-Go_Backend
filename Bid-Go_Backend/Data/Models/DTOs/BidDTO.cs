using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class BidDTO
    {
        public int BidId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }

        [Required]
        public DateTime DeliveryDeadline { get; set; }

        public int DriverId { get; set; }  

        public int TransportRequestId { get; set; } 

        public DriverDTO Driver { get; set; }
    }
}
