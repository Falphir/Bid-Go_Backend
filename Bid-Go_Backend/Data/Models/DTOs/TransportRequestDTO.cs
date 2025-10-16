using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class CreateTransportRequestDTO
    {
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string Package { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public decimal Volume { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public DateTime PickupDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string Image { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }

    public class UpdateTransportRequestDTO
    {
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        public DateTime? PickupDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Volume { get; set; }
        public string? Image { get; set; }
    }

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
