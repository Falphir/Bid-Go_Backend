using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using IAuthorizationService = Bid_Go_Backend.Services.Interfaces.IAuthorizationService;

namespace Bid_Go_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _payments;
        private readonly IAuthorizationService _authorizationService;

        public PaymentsController(IPaymentService payments, IAuthorizationService authorizationService)
        {
            _payments = payments;
            _authorizationService = authorizationService;
        }

        /// <summary>
        /// Process a payment for a transport request using the request's selected bid.
        /// </summary>
        /// <remarks>
        /// Permission checks are performed at the controller level; the payment gateway interaction happens in the payment service.
        /// </remarks>
        /// <param name="dto">Payment request payload</param>
        /// <returns>Payment result or error information.</returns>
        [Authorize(Policy = "CompanyOnly")]
        [HttpPost]
        public async Task<IActionResult> ProcessPayment([FromBody] CreatePaymentRequestDTO dto)
        {

            var companyId = int.Parse(User.FindFirst("userId")!.Value);

            var hasPermission = await _authorizationService.CompanyOwnsTransportRequestAsync(companyId, dto.TransportRequestId);
            if (!hasPermission)
                return Forbid();


            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _payments.ProcessPaymentAsync(dto);

            if (result.Status == EPaymentStatus.Failed)
            {
                var payload = new ApiMessageResponse<PaymentResultDTO>
                {
                    message = "Payment was declined by the payment gateway.",
                    payment = result
                };
                return BadRequest(payload);
            }

            return Ok(new ApiMessageResponse<PaymentResultDTO>
            {
                message = "Payment successfully processed.",
                payment = result
            });
        }

        /// <summary>
        /// Get payments associated with a user (company or driver).
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns>List of payments</returns>
        [Authorize(Policy = "CompanyOnly")]
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetPaymentsByUser(int userId)
        {
            var result = await _payments.GetPaymentsByUserAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Retry a failed payment using a new Stripe token.
        /// </summary>
        /// <param name="paymentId">Payment identifier</param>
        /// <param name="dto">Retry payload containing a Stripe token</param>
        /// <returns>Result of retry attempt</returns>
        [Authorize(Policy = "CompanyOnly")]
        [HttpPost("{paymentId:int}/retry")]
        public async Task<IActionResult> Retry(int paymentId, [FromBody] RetryPaymentRequestDTO dto)
        {

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (ok, error, result) = await _payments.RetryPaymentAsync(paymentId, dto.StripeToken);

            if (ok)
                return Ok(new ApiMessageResponse<PaymentResultDTO> { message = "Payment completed on retry.", payment = result! });

            return BadRequest(new ApiMessageResponse<PaymentResultDTO>
            {
                message = error ?? "Retry was executed, but the payment was not completed.",
                payment = result!
            });
        }

    }
}
