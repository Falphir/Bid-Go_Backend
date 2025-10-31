using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models
{
    public class AutomaticSelectionResult
    {
        public Bid? SelectedBid { get; set; }
        public string? Message { get; set; }
        public bool Success => SelectedBid != null;
    }

}
