using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bid_Go_Backend.Data.Models.DTOs
{
    public class ResetPasswordRequestDTO
    {
        public string Token { get; set; } = string.Empty;
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must have at least 8 characters, including an uppercase letter, a lowercase letter, a number, and a special character.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
