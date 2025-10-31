using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class BidHistoryDTO
    {
        public required string CompanyName { get; set; }
        public required string Package { get; set; }     
        public DateTime Date { get; set; }                    
        public required string Destination { get; set; }
        public decimal Value { get; set; }                    
        public required EBidStatus Status { get; set; }  
        public decimal? Rating { get; set; }
    }
}
