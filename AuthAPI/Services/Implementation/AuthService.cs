using AuthAPI.Data;
using AuthAPI.DTOs;
using AuthAPI.Models;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Services.Implementation
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        public AuthService(AppDbContext context, IConfiguration configuration, IMapper mapper)
        {
            _context = context;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<string> RegisterAsync(RegisterRequestDto registerRequest)
        {
            // 1. Check if the user already exists
            var existingUser = await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == registerRequest.Email);
            if (existingUser != null)
            {
                return "User with this email already exists.";
            }

            // 2. Map DTO to Entity
            var user = _mapper.Map<ApplicationUser>(registerRequest);

            // 3. Hash the password securely
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerRequest.Password);

            // 4. Save to Database
            _context.ApplicationUsers.Add(user);
            await _context.SaveChangesAsync();

            return string.Empty; // Empty string means success
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest)
        {
            // 1. Find user in the database
            var user = await _context.ApplicationUsers.FirstOrDefaultAsync(u => u.Email == loginRequest.Email);

            // 2. Verify the user exists and the password matches the hash
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
            {
                return null; // Invalid credentials
            }

            // 3. Generate JWT and return
            var token = GenerateJwtToken(user);
            var responseDto = _mapper.Map<LoginResponseDto>(user);
            responseDto.Token = token;

            return responseDto;
        }

        private string GenerateJwtToken(ApplicationUser user)
        {
            var secretKey = _configuration.GetValue<string>("JwtOptions:Secret");
            var issuer = _configuration.GetValue<string>("JwtOptions:Issuer");
            var audience = _configuration.GetValue<string>("JwtOptions:Audience");

            var key = Encoding.ASCII.GetBytes(secretKey!);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(2),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
