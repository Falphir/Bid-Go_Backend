using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class ChatDTO
    {
        public int ChatId { get; set; }
        public int TransportRequestId { get; set; }
        public EChatStatus Status { get; set; }
        public List<MessageDTO> Messages { get; set; }
    }
}
