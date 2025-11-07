using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models
{
    public class ReviewCompany : Review
    {
        [Required]
        public int ServiceQuality { get; set; }

        [Required]
        public int ClientSuport { get; set; }
    }
}
