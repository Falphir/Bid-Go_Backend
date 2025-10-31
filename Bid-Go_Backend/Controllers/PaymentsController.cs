using Bid_Go_Backend.Data;
using Bid_Go_Backend.Data.Models;
using Bid_Go_Backend.Data.Models.DTOs;
using Bid_Go_Backend.Data.Models.Enums;
using Bid_Go_Backend.Data.Repositories.Interfaces;
using Bid_Go_Backend.Repositories.BidRepo;
using Bid_Go_Backend.Repositories.Interface;
using Castle.Components.DictionaryAdapter.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentRepository _paymentRepo;
        public PaymentsController(IPaymentRepository paymentRepo)
        {
            _paymentRepo = paymentRepo;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment([FromBody] CreatePaymentRequestDTO dto)
        {
            var result = await _paymentRepo.ProcessPaymentAsync(dto);

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


        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetPaymentsByUser(int userId)
        {
            var result = await _paymentRepo.GetPaymentsByUserAsync(userId);
            return Ok(result);
        }

        [HttpPost("{paymentId:int}/retry")]
        public async Task<IActionResult> Retry(int paymentId, [FromBody] RetryPaymentRequestDTO dto)
        {
            try
            {
                var result = await _paymentRepo.RetryPaymentAsync(paymentId, dto.StripeToken);

                if (result.Status == EPaymentStatus.Confirmed)
                {
                    return Ok(new
                    {
                        message = "Payment completed on retry.",
                        payment = result
                    });
                }

                return BadRequest(new
                {
                    message = "Retry was executed, but the payment was not completed.",
                    payment = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
