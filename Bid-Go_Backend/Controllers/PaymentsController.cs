using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Repositories.BidRepo;
using Bid_Go_Backend.Repositories.Interface;
using Bid_Go_Backend.Services.Interfaces;
using Castle.Components.DictionaryAdapter.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _payments;

        public PaymentsController(IPaymentService payments)
        {
            _payments = payments;
        }

        /// <summary>
        /// Processa um pagamento para o TransportRequest (usa o SelectedBid do TR).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ProcessPayment([FromBody] CreatePaymentRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _payments.ProcessPaymentAsync(dto);

            if (result.Status == EPaymentStatus.Failed)
            {
                return BadRequest(new
                {
                    message = "Payment was declined by the payment gateway.",
                    payment = result
                });
            }

            return Ok(new
            {
                message = "Payment successfully processed.",
                payment = result
            });
        }

        /// <summary>
        /// Lista pagamentos por utilizador (CompanyId ou DriverId).
        /// </summary>
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetPaymentsByUser(int userId)
        {
            var result = await _payments.GetPaymentsByUserAsync(userId);
            return Ok(result);
        }

        /// <summary>
        /// Faz retry de um pagamento existente.
        /// </summary>
        [HttpPost("{paymentId:int}/retry")]
        public async Task<IActionResult> Retry(int paymentId, [FromBody] RetryPaymentRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (ok, error, result) = await _payments.RetryPaymentAsync(paymentId, dto.StripeToken);

            if (ok)
                return Ok(new { message = "Payment completed on retry.", payment = result });

            return BadRequest(new
            {
                message = error ?? "Retry was executed, but the payment was not completed.",
                payment = result
            });
        }

    }
}
