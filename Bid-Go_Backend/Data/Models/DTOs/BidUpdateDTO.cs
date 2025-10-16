using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    namespace Bid_Go_Backend.Data.Models.DTOs
    {
        public class BidUpdateDTO
        {
            [Key]

            [Required]
            [Column(TypeName = "decimal(18,2)")]
            public decimal Value { get; set; }

            [Required]
            public DateTime DeliveryDeadline { get; set; }

        }
    }