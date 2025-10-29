using System.ComponentModel.DataAnnotations;

namespace Bid_Go_Backend.Data.Models.DTOs.CompanyDTOs
{
    public class RegisterCompanyDTO
    {
        [Required(ErrorMessage = "O nome é obrigatório.")]
        [MaxLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "O nome da empresa é obrigatório.")]
        [MaxLength(100, ErrorMessage = "O nome da empresa não pode exceder 100 caracteres.")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "A morada é obrigatória.")]
        [MaxLength(150, ErrorMessage = "A morada não pode exceder 150 caracteres.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "O email é obrigatório.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            ErrorMessage = "O email deve ter um formato válido (ex: nome@dominio.com).")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A password é obrigatória.")]
        [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "A password deve ter no mínimo 8 caracteres, incluindo uma maiúscula, uma minúscula, um número e um carácter especial.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "O número de telefone é obrigatório.")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "O número de telefone deve conter exatamente 9 dígitos.")]
        public int PhoneNumber { get; set; }

        [Required(ErrorMessage = "O NIF é obrigatório.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "O NIF deve conter exatamente 10 dígitos.")]
        public int NIF { get; set; }
    }
}
