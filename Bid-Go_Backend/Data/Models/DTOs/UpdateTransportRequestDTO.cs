using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class UpdateTransportRequestDTO
    {
        public string? Origin { get; set; }
        public string? Destination { get; set; }
        public string? Package { get; set; }
        public DateTime? PickupDate { get; set; }
        public DateTime? DeliveryDate { get; set; }

        public bool? IsAutomaticSelectionEnabled { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Volume { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public DateTime? BiddingStartDate { get; set; }
        public DateTime? BiddingEndDate { get; set; }
        public decimal? MaxPrice { get; set; }


    }
}