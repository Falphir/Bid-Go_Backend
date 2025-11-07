using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models
{
    public class ReviewDriver : Review
    {
        [Required]
        [Range(0,5)]
        public int Punctuality { get; set; }

        [Required]
        [Range(0, 5)]
        public int Behavior { get; set; }
    }
}
