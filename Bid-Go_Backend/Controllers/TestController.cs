using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bid_Go_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        // Este endpoint só é acessível com token JWT válido
        [Authorize]
        [HttpGet("me")]
        public IActionResult GetAuthenticatedUser()
        {
            // Podes recuperar dados do token (claims)
            var userId = User.FindFirst("userId")?.Value;
            var email = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            var userType = User.FindFirst("userType")?.Value;
            var role = User.FindFirst("role")?.Value;

            return Ok(new
            {
                message = "Token JWT válido!",
                userId,
                email,
                userType,
                role
            });
        }

        // Endpoint público, sem autenticação
        [AllowAnonymous]
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "Servidor ativo 🚀 (sem token)" });
        }
    }
}
