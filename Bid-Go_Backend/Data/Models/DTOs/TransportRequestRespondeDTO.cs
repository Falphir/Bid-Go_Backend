using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class TransportRequestResponseDTO
    {
        public int Id { get; set; }
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public DateTime PickupDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string Image { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
