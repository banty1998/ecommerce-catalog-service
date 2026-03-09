using AuthAPI.DTOs;
using AuthAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto dto)
        {
            var errorMessage = await _authService.RegisterAsync(dto);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                return BadRequest(new { Message = errorMessage });
            }

            return Ok(new { Message = "Registration successful" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            var response = await _authService.LoginAsync(dto);

            if (response == null)
            {
                return Unauthorized(new { Message = "Invalid email or password." });
            }

            return Ok(response);
        }
    }
}
