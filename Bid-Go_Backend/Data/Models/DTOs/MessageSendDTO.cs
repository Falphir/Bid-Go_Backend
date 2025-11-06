using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class MessageSentDTO
    {
        public string Context { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; }
 
    }
}
