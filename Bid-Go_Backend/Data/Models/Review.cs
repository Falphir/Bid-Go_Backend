using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models
{
    public abstract class Review
    {
        [Key]
        public int ReviewId { get; set; }
        [Required]
        public DateTime TimeStamp { get; set; } = DateTime.Now;

        [Required]
        [Column(TypeName = "decimal(3,2)")]
        public decimal Classification {  get; set; }

        [ForeignKey(nameof(Driver))]
        public int DriverId { get; set; }
        public Driver Driver { get; set; } = null;

        [ForeignKey(nameof(Company))]
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null;

        //Relação 1:N com TransportRequest
        [Required]
        [ForeignKey(nameof(TransportRequest))]
        public int TransportRequestId { get; set; }
        public TransportRequest TransportRequest { get; set; } = null!;

    }
}
