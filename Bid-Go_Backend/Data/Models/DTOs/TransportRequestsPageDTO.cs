using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class TransportRequestsPageDTO
    {
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string Package { get; set; }
        public DateTime PickupDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string Image { get; set; }
        public decimal MaxPrice { get; set; }
    }
}
