using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

        public int ChatId { get; set; }

        public Chats Chat { get; set; } = null!;

        [ForeignKey(nameof(Driver))]
        public int DriverId { get; set; }
        public Driver? Driver { get; set; } = null;

        [ForeignKey(nameof(Company))]
        public int CompanyId { get; set; }
        public Company? Company { get; set; } = null;
    }
}
