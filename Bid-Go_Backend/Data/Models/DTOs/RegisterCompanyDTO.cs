using System.ComponentModel.DataAnnotations;

namespace Bid_Go_Backend.Data.Models.DTOs.CompanyDTOs
{
    public class RegisterCompanyDTO
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Company name is required.")]
        [MaxLength(100, ErrorMessage = "Company name cannot exceed 100 characters.")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [MaxLength(150, ErrorMessage = "Address cannot exceed 150 characters.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            ErrorMessage = "Email must have a valid format (e.g. name@domain.com).")]
        [MaxLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must have at least 8 characters, including an uppercase letter, a lowercase letter, a number, and a special character.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "Phone number must contain exactly 9 digits.")]
        public int PhoneNumber { get; set; }

        [Required(ErrorMessage = "Tax ID (NIF) is required.")]
        [RegularExpression(@"^\d{9,10}$", ErrorMessage = "Tax ID (NIF) must contain between 9 and 10 digits.")]
        public int NIF { get; set; }
    }
}
