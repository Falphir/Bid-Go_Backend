using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }
        [Required]
        [MaxLength(256)]
        public string Context { get; set; } = string.Empty;
        [Required]
        public DateTime TimeStamp { get; set; }
        [Required]
        public ENotificationType Type  { get; set; } 
    }
}
