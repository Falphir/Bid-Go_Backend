using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models
{
    public class TransportRequest
    {
        [Key]
        public int TransporRequestId { get; set; }

        [Required]
        public string Origin { get; set; } = string.Empty;

        [Required]
        public string Destiny { get; set; } = string.Empty;

        [Required]
        public string Package { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Weight { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Volume { get; set; }

        [Required]
        public DateTime CollectDate { get; set; }

        [Required]
        public DateTime DeliveryDate { get; set; }

        [Required]
        public string Image { get; set; } = string.Empty;

        [Required]
        public ERequestStatus Status { get; set; }

        [ForeignKey(nameof(Company))]
        public int CompanyId { get; set; }
        public Company? Company { get; set; } = null;
    }
}
