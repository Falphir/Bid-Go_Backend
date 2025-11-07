using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Services.Payments
{
    public sealed class ChargeResult
    {
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }
        public ChargeResult(bool success, string? errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
}
