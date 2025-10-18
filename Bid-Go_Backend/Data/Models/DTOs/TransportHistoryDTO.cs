using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class TransportHistoryDTO
    {
        public int RequestId { get; set; } 
        public required string Package { get; set; }
        public required string DriverName { get; set; } 
        public DateTime Date { get; set; }        
        public required string Destination { get; set; }
        public decimal Price { get; set; }
        public required string Status { get; set; }

    }
}
