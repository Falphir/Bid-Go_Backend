using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class ChatMessageDTO
    {
        public int Id { get; set; }
        public string Context { get; set; }
        public DateTime TimeStamp { get; set; }
        public int ChatId { get; set; }
        public int? DriverId { get; set; }
        public int? CompanyId { get; set; }
    }
}
