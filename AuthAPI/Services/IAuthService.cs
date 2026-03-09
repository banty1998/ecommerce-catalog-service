using AuthAPI.DTOs;

namespace AuthAPI.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest);
        Task<string> RegisterAsync(RegisterRequestDto registerRequest);
    }
}
