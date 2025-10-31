using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class TransportRequestResponseDTO
    {
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string Package { get; set; }
        public DateTime PickupDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public decimal Weight { get; set; }
        public decimal Volume { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public string Image { get; set; }
        public decimal MaxPrice { get; set; }
        public ERequestStatus Status { get; internal set; }
    }
}