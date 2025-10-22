using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class DriverProfileDTO
    {
        public string? Name { get; set; } 
        public string? Email { get; set; }
        public int? PhoneNumber { get; set; }
        public int? NIF { get; set; }
        public string? DriverLicense { get; set; }
        public string? Insurance { get; set; }
    }
}
