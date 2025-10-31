using Bid_Go_Backend.Data.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models
{
    public class TransportRequest
    {
        [Key]
        public int TransportRequestId { get; set; }

        [Required]
        public string Origin { get; set; } = string.Empty;

        [Required]
        public string Destination { get; set; } = string.Empty;

        [Required]
        public string Package { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Weight { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Volume { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Length { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Width { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Height { get; set; }

        [Required]
        public DateTime PickupDate { get; set; }

        [Required]
        public DateTime DeliveryDate { get; set; }

        [Required]
        public string Image { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxPrice { get; set; }

        [Required]
        public ERequestStatus Status { get; set; }

        [ForeignKey(nameof(Company))]
        public int CompanyId { get; set; }
        public Company? Company { get; set; } = null;
       
        [JsonIgnore]
        public Chats Chat { get; set; } = null!;

        //Relação 1:1 com Payment
        public Payment? Payment { get; set; }

        public ICollection<Bid> Bids { get; set; } = new List<Bid>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
