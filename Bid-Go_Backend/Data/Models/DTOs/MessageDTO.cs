using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class MessageDTO
    {
        public string Context { get; set; } = string.Empty;
        public int? DriverId { get; set; }
        public int? CompanyId { get; set; }
    }
}
