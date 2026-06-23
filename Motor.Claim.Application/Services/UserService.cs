using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Motor.Claim.Application.Dtos.Auth;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.Domain.Entities;
using Motor.Claim.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Motor.Claim.Application.Services
{
    public class UserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IWorkshopRepository _workshopRepository;
        private readonly IEmailNotificationService _emailNotificationService;
        private readonly PasswordHasher<UserEntity> _passwordHasher;
        private readonly IConfiguration _configuration;

        public UserService(
            IUserRepository userRepository,
            IWorkshopRepository workshopRepository,
            IEmailNotificationService emailNotificationService,
            IConfiguration configuration)
        {
            _userRepository = userRepository;
            _workshopRepository = workshopRepository;
            _emailNotificationService = emailNotificationService;
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

            if (!user.IsActive)
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
                IsActive = user.IsActive,
                WorkshopId = user.WorkshopId,
                WorkshopName = workshopName
            };
        }

        public async Task RequestPasswordResetAsync(ForgotPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return;
            }

            var user = await _userRepository.GetByEmailAsync(request.Email.Trim());
            if (user == null)
            {
                return;
            }

            var token = GenerateSecureToken();
            user.PasswordResetTokenHash = HashToken(token);
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
            await _userRepository.UpdateAsync(user);

            var resetLink = BuildResetPasswordLink(token);
            var htmlBody = BuildPasswordResetEmailBody(user.FullName, resetLink);

            await _emailNotificationService.SendAsync(
                user.Email,
                "Reset your Motor Claim password",
                htmlBody);
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token)
                || string.IsNullOrWhiteSpace(request.NewPassword)
                || string.IsNullOrWhiteSpace(request.ConfirmPassword))
            {
                return false;
            }

            if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
            {
                throw new ArgumentException("Passwords do not match.");
            }

            if (request.NewPassword.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters.");
            }

            var tokenHash = HashToken(request.Token.Trim());
            var user = await _userRepository.GetByPasswordResetTokenHashAsync(tokenHash);

            if (user == null
                || user.PasswordResetTokenExpiresAt == null
                || user.PasswordResetTokenExpiresAt <= DateTime.UtcNow)
            {
                return false;
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
            user.PasswordResetTokenHash = null;
            user.PasswordResetTokenExpiresAt = null;
            await _userRepository.UpdateAsync(user);

            return true;
        }

        public async Task<UserProfileResponse?> GetProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdWithWorkshopAsync(userId);
            if (user == null)
            {
                return null;
            }

            return MapProfile(user);
        }

        public async Task<List<UserProfileResponse>> GetUsersAsync(UserRole? role, bool? isActive)
        {
            var users = await _userRepository.GetUsersAsync(role, isActive);
            return users.Select(MapProfile).ToList();
        }

        public async Task<UserProfileResponse> UpdateUserAccountAsync(Guid userId, UpdateUserAccountRequest request)
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

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                throw new ArgumentException("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(request.MobileNumber))
            {
                throw new ArgumentException("Mobile number is required.");
            }

            var normalizedEmail = request.Email.Trim();
            var existingUser = await _userRepository.GetByEmailAsync(normalizedEmail);
            if (existingUser != null && existingUser.UserId != userId)
            {
                throw new ArgumentException("Email already exists.");
            }

            if (request.Role == UserRole.PanelWorkshop)
            {
                if (!request.WorkshopId.HasValue)
                {
                    throw new ArgumentException("WorkshopId is required for PanelWorkshop users.");
                }

                var workshop = await _workshopRepository.GetByIdAsync(request.WorkshopId.Value);
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

            user.FullName = request.FullName.Trim();
            user.Email = normalizedEmail;
            user.MobileCountry = request.MobileCountry;
            user.MobileNumber = request.MobileNumber.Trim();
            user.Role = request.Role;
            user.WorkshopId = request.Role == UserRole.PanelWorkshop ? request.WorkshopId : null;
            user.IsActive = request.IsActive;

            await _userRepository.UpdateAsync(user);

            return (await GetProfileAsync(userId))!;
        }

        public async Task<UserProfileResponse> SetUserActiveStatusAsync(Guid userId, bool isActive)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found.");
            }

            user.IsActive = isActive;
            await _userRepository.UpdateAsync(user);

            return (await GetProfileAsync(userId))!;
        }

        private static UserProfileResponse MapProfile(UserEntity user)
        {
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
                IsActive = user.IsActive,
                WorkshopId = user.WorkshopId,
                Workshop = user.Workshop == null
                    ? null
                    : new Motor.Claim.Application.Dtos.Workshop.WorkshopResponse
                    {
                        WorkshopId = user.Workshop.WorkshopId,
                        Name = user.Workshop.Name,
                        State = user.Workshop.State,
                        Address = user.Workshop.Address,
                        Phone = DeserializeOptionalList(user.Workshop.Phone),
                        Fax = user.Workshop.Fax,
                        Email = DeserializeOptionalList(user.Workshop.Email),
                        BankName = user.Workshop.BankName,
                        BankAccountNumber = user.Workshop.BankAccountNumber,
                        BankAccountHolderName = user.Workshop.BankAccountHolderName,
                        StripeConnectedAccountId = user.Workshop.StripeConnectedAccountId,
                        StripeOnboardingStatus = user.Workshop.StripeOnboardingStatus,
                        StripeChargesEnabled = user.Workshop.StripeChargesEnabled,
                        StripePayoutsEnabled = user.Workshop.StripePayoutsEnabled,
                        StripeLastSyncedAt = user.Workshop.StripeLastSyncedAt,
                        IsPanelWorkshop = user.Workshop.IsPanelWorkshop,
                        IsActive = user.Workshop.IsActive
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

        private static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        private string BuildResetPasswordLink(string token)
        {
            var resetPasswordUrl = _configuration["Frontend:ResetPasswordUrl"];
            if (string.IsNullOrWhiteSpace(resetPasswordUrl))
            {
                resetPasswordUrl = "http://localhost:3000/reset-password";
            }

            var separator = resetPasswordUrl.Contains('?') ? "&" : "?";
            return $"{resetPasswordUrl}{separator}token={WebUtility.UrlEncode(token)}";
        }

        private static string BuildPasswordResetEmailBody(string fullName, string resetLink)
        {
            var safeName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName.Trim());
            var safeLink = WebUtility.HtmlEncode(resetLink);

            return $@"
<div style=""font-family: Arial, sans-serif; color: #222; line-height: 1.5;"">
  <p>Hi {safeName},</p>
  <p>We received a request to reset your Motor Claim password.</p>
  <p>
    <a href=""{safeLink}"" style=""display: inline-block; padding: 10px 16px; background: #0f5fff; color: #fff; text-decoration: none; border-radius: 4px;"">
      Reset password
    </a>
  </p>
  <p>This link expires in 1 hour. If you did not request this, you can ignore this email.</p>
</div>";
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
