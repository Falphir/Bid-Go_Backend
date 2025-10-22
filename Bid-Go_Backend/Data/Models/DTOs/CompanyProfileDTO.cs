using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class CompanyProfileDTO
    {
        public string? Name { get; set; } 
        public string? Email { get; set; } 
        public int? PhoneNumber { get; set; }
        public int? NIF { get; set; }
        public string? CompanyName { get; set; } 
        public string? Address { get; set; } 
    }
}
