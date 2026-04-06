using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Motor.Claim.Application.Dtos.Auth;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Motor.Claim.Application.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly PasswordHasher<UserEntity> _passwordHasher;
        private readonly IConfiguration _configuration;

        public UserService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<UserEntity>();
        }

        public async Task<UserEntity> RegisterAsync(RegisterUserRequest request)
        {
            return await RegisterAsync(request, UserRole.Customer);
        }

        public async Task<UserEntity> RegisterAsync(RegisterUserRequest request, UserRole role)
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
            {
                throw new ArgumentException("Email already exists.");
            }

            if (request.IdType == IdType.NRIC)
            {
                if (string.IsNullOrWhiteSpace(request.NRIC))
                {
                    throw new ArgumentException("NRIC is required when ID Type is NRIC.");
                }

                request.PassportNo = null;
                request.IssueCountry = null;
            }

            if (request.IdType == IdType.Passport)
            {
                if (string.IsNullOrWhiteSpace(request.PassportNo))
                {
                    throw new ArgumentException("Passport number is required when ID Type is Passport.");
                }

                if (string.IsNullOrWhiteSpace(request.IssueCountry))
                {
                    throw new ArgumentException("Issue country is required when ID Type is Passport.");
                }

                request.NRIC = null;
            }

            var user = new UserEntity
            {
                CreatedAt = DateTime.Now,
                UserId = Guid.NewGuid(),
                FullName = request.FullName,
                IdType = request.IdType,
                NRIC = request.NRIC,
                PassportNo = request.PassportNo,
                IssueCountry = request.IssueCountry,
                MobileCountry = request.MobileCountry,
                MobileNumber = request.MobileNumber,
                Email = request.Email,
                IsMaybankGroupEmployee = request.IsMaybankGroupEmployee,
                Role = role
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            return await _userRepository.AddAsync(user);
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return null;
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                return null;
            }

            var token = GenerateJwtToken(user);

            return new LoginResponse
            {
                Token = token,
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            };
        }

        private string GenerateJwtToken(UserEntity user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new System.Security.Claims.Claim("UserId", user.UserId.ToString()),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Email, user.Email),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.UniqueName, user.FullName),
                new System.Security.Claims.Claim(ClaimTypes.Role, user.Role.ToString()),
                new System.Security.Claims.Claim("role", user.Role.ToString()),
                new System.Security.Claims.Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(jwtSettings["ExpireMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
