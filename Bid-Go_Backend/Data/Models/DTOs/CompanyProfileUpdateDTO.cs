using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Bid_Go_Backend.Data.Models.DTOs
{
  
        public class CompanyProfileUpdateDTO
        {
            [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
            public string? Name { get; set; }
            public IFormFile? ProfileImage { get; set; }

            [MaxLength(100, ErrorMessage = "Company name cannot exceed 100 characters.")]
            public string? CompanyName { get; set; }

            [MaxLength(150, ErrorMessage = "Address cannot exceed 150 characters.")]
            public string? Address { get; set; }

            [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            ErrorMessage = "Email must have a valid format (e.g. name@domain.com).")]
            [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
            public string? Email { get; set; } = string.Empty;

            [RegularExpression(@"^\d{9}$", ErrorMessage = "Phone number must contain exactly 9 digits.")]
            public int? PhoneNumber { get; set; }

            [RegularExpression(@"^\d{9,10}$", ErrorMessage = "Tax ID (NIF) must contain between 9 and 10 digits.")]
            public int? NIF { get; set; }
        }

    }