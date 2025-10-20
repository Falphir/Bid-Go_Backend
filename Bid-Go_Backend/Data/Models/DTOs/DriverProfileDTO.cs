using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class DriverProfileDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int PhoneNumber { get; set; }
        public int NIF { get; set; }
        public string DriverLicense { get; set; } = string.Empty;
        public string Insurance { get; set; } = string.Empty;
    }
}
