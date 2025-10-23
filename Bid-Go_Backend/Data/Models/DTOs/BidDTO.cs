using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class BidDTO
    {
        public int BidId { get; set; }
        public int TransportRequestId { get; set; }
        public decimal Value { get; set; }
        public DateTime DeliveryDeadline { get; set; }
        public int Status { get; set; }

        public DriverDTO Driver { get; set; }
    }
}
