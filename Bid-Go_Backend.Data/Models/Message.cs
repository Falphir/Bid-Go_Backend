using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Context { get; set; } = string.Empty;

        [Required]
        public DateTime TimeStamp { get; set; }

        [Required]
        public int ChatId { get; set; }

        public Chat Chat { get; set; } = null!;

    }
}
