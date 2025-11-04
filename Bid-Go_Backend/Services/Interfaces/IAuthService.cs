using Bid_Go_Backend.Data.Models;


namespace Bid_Go_Backend.Services.Interfaces
{
    public interface IAuthService
    {

        Task<(bool Success, string Message, string Token, DateTime? Expiration)> LoginAsync(string email, string password);

      
        Task<(int StatusCode, string Message)> RecoverPasswordAsync(string email);

       
        Task<(int StatusCode, string Message)> ResetPasswordAsync(string token, string newPassword);

    }
}
