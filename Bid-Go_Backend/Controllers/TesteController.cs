using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TesteController : ControllerBase
    {
        // Endpoint protegido com JWT
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var userId = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            var userType = User.Claims.FirstOrDefault(c => c.Type == "userType")?.Value;

            return Ok(new
            {
                message = "Endpoint protegido",
                email = userEmail,
            });
        }

        // Apenas Companys podem aceder
        [HttpGet("company-endpoint")]
        [Authorize(Policy = "CompanyOnly")]
        public IActionResult CompanyEndpoint()
        {
            return Ok(new { message = "Apenas Companies conseguem ver isto!" });
        }

        // Apenas Drivers podem aceder
        [HttpGet("driver-endpoint")]
        [Authorize(Policy = "DriverOnly")]
        public IActionResult DriverEndpoint()
        {
            return Ok(new { message = "Apenas Drivers conseguem ver isto!" });
        }
    }
}
