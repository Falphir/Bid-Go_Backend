using Bid_Go_Backend.Data.Models;


namespace Bid_Go_Backend.Services.Interfaces
{
    /// <summary>
    /// Handles authentication-related operations such as login and password recovery.
    /// Keep methods focused on high-level flows; implementation details (token creation, email sending) belong to service implementations.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Validate credentials and return a JWT token when successful.
        /// </summary>
        /// <param name="email">User email address used for login.</param>
        /// <param name="password">Plain-text password to validate.</param>
        /// <returns>
        /// A tuple containing Success flag, Message text, Token string and Expiration timestamp when authentication succeeds.
        /// </returns>
        Task<(bool Success, string Message, string Token, DateTime? Expiration)> LoginAsync(string email, string password);

        /// <summary>
        /// Initiate password recovery for the specified email address.
        /// </summary>
        /// <param name="email">Email to send recovery instructions to.</param>
        /// <returns>A status code and a human-readable message describing the outcome.</returns>
        Task<(int StatusCode, string Message)> RecoverPasswordAsync(string email);

        /// <summary>
        /// Reset a password using a recovery token and a new password.
        /// </summary>
        /// <param name="token">Recovery token previously issued to the user.</param>
        /// <param name="newPassword">New plain-text password to set.</param>
        /// <returns>A status code and message describing the outcome.</returns>
        Task<(int StatusCode, string Message)> ResetPasswordAsync(string token, string newPassword);

    }
}
