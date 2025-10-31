using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class ReviewByServiceDTO
    {
        public DateTime TimeStamp { get; set; } = DateTime.Now;
        public decimal Classification { get; set; }
        public required string Name { get; set; } // Name de quem dá a review
        public int? Punctuality { get; set; } 
        public int? Behavior { get; set; }
        public int? ServiceQuality { get; set; }
        public int? ClientSuport { get; set; }
    }
}
