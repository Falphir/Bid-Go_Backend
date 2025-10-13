using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models
{
    public class Company : User
    {
        [Required]
        [MaxLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Address { get; set; } = string.Empty;
    }
}
