using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class ReviewRequestServiceDTO
    {
        public DateTime TimeStamp { get; set; } = DateTime.Now;
        public decimal Classification { get; set; }
        public int DriverId { get; set; }
        public int CompanyId { get; set; }
        public int TransportRequestId { get; set; }
        public required string Discriminator { get; set; }
        public int Punctuality { get; set; }
        public int Behavior { get; set; }
        public int ServiceQuality { get; set; }
        public int ClientSuport { get; set; }
    }
}
