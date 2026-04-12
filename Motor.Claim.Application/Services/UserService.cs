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
        private readonly IWorkshopRepository _workshopRepository;
        private readonly PasswordHasher<UserEntity> _passwordHasher;
        private readonly IConfiguration _configuration;

        public UserService(
            IUserRepository userRepository,
            IWorkshopRepository workshopRepository,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _workshopRepository = workshopRepository;
            _configuration = configuration;
            _passwordHasher = new PasswordHasher<UserEntity>();
        }

        public async Task<UserEntity> RegisterAsync(RegisterUserRequest request)
        {
            return await RegisterAsync(request, UserRole.Customer);
        }

        public async Task<UserEntity> RegisterAsync(RegisterUserRequest request, UserRole role)
        {
            return await RegisterAsync(request, role, null);
        }

        public async Task<UserEntity> RegisterAsync(RegisterUserRequest request, UserRole role, Guid? workshopId)
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

            if (role == UserRole.PanelWorkshop && !workshopId.HasValue)
            {
                throw new ArgumentException("WorkshopId is required for PanelWorkshop users.");
            }

            if (role == UserRole.PanelWorkshop)
            {
                var workshop = await _workshopRepository.GetByIdAsync(workshopId!.Value);

                if (workshop == null)
                {
                    throw new ArgumentException("Selected workshop does not exist.");
                }

                if (!workshop.IsPanelWorkshop)
                {
                    throw new ArgumentException("Selected workshop is not marked as a panel workshop.");
                }

                if (!workshop.IsActive)
                {
                    throw new ArgumentException("Selected workshop is not active.");
                }
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
                Role = role,
                WorkshopId = role == UserRole.PanelWorkshop ? workshopId : null
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
            var workshopName = user.WorkshopId.HasValue
                ? (await _workshopRepository.GetByIdAsync(user.WorkshopId.Value))?.Name
                : null;

            return new LoginResponse
            {
                Token = token,
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                WorkshopId = user.WorkshopId,
                WorkshopName = workshopName
            };
        }

        public async Task<UserProfileResponse?> GetProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            var workshop = user.WorkshopId.HasValue
                ? await _workshopRepository.GetByIdAsync(user.WorkshopId.Value)
                : null;

            return new UserProfileResponse
            {
                UserId = user.UserId,
                FullName = user.FullName,
                IdType = user.IdType,
                Nric = user.NRIC,
                PassportNo = user.PassportNo,
                IssueCountry = user.IssueCountry,
                MobileCountry = user.MobileCountry,
                MobileNumber = user.MobileNumber,
                Email = user.Email,
                IsMaybankGroupEmployee = user.IsMaybankGroupEmployee,
                Role = user.Role,
                WorkshopId = user.WorkshopId,
                Workshop = workshop == null
                    ? null
                    : new Motor.Claim.Application.Dtos.Workshop.WorkshopResponse
                    {
                        WorkshopId = workshop.WorkshopId,
                        Name = workshop.Name,
                        State = workshop.State,
                        Address = workshop.Address,
                        Phone = DeserializeOptionalList(workshop.Phone),
                        Fax = workshop.Fax,
                        Email = DeserializeOptionalList(workshop.Email),
                        BankName = workshop.BankName,
                        BankAccountNumber = workshop.BankAccountNumber,
                        BankAccountHolderName = workshop.BankAccountHolderName,
                        IsPanelWorkshop = workshop.IsPanelWorkshop,
                        IsActive = workshop.IsActive
                    }
            };
        }

        public async Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateMyProfileRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found.");
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                throw new ArgumentException("Full name is required.");
            }

            if (string.IsNullOrWhiteSpace(request.MobileNumber))
            {
                throw new ArgumentException("Mobile number is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required.");
            }

            var normalizedEmail = request.Email.Trim();
            var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (existingUser != null && existingUser.UserId != userId)
            {
                throw new ArgumentException("Email already exists.");
            }

            if (request.IdType == IdType.NRIC)
            {
                if (string.IsNullOrWhiteSpace(request.Nric))
                {
                    throw new ArgumentException("NRIC is required when ID Type is NRIC.");
                }

                user.NRIC = request.Nric.Trim();
                user.PassportNo = null;
                user.IssueCountry = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.PassportNo))
                {
                    throw new ArgumentException("Passport number is required when ID Type is Passport.");
                }

                if (string.IsNullOrWhiteSpace(request.IssueCountry))
                {
                    throw new ArgumentException("Issue country is required when ID Type is Passport.");
                }

                user.NRIC = null;
                user.PassportNo = request.PassportNo.Trim();
                user.IssueCountry = request.IssueCountry.Trim();
            }

            user.FullName = request.FullName.Trim();
            user.IdType = request.IdType;
            user.MobileCountry = request.MobileCountry;
            user.MobileNumber = request.MobileNumber.Trim();
            user.Email = normalizedEmail;

            await _userRepository.UpdateAsync(user);

            return (await GetProfileAsync(userId))!;
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
                new System.Security.Claims.Claim("WorkshopId", user.WorkshopId?.ToString() ?? string.Empty),
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

        private static List<string> DeserializeOptionalList(string? payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return new List<string>();
            }

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(payload) ?? new List<string>();
            }
            catch
            {
                return new List<string> { payload.Trim() };
            }
        }
    }
}
