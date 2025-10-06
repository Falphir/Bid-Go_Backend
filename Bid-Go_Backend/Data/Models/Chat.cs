using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models
{
    public class Chat
    {
        [Key]
        public int ChatId { get; set; }
        [Required]
        public EChatStatus Status { get; set; }

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
