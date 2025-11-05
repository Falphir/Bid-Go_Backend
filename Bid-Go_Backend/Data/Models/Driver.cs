using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models
{
    public class Driver : User
    {
        [Required]
        [MaxLength(512)]
        public string DriverLicense { get; set; } = string.Empty;

        [Required]
        [MaxLength(512)]
        public string Insurance { get; set; } = string.Empty;

        public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    }
}
