using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class BidWithDriverDTO
    {
        public int BidId { get; set; }
        public decimal Value { get; set; }
        public int DriverId { get; set; }

        public DateTime Deadline { get; set; }
        public string DriverName { get; set; }
        public string DriverEmail { get; set; }
    }
}
