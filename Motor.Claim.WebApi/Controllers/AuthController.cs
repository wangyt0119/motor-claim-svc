using Microsoft.AspNetCore.Mvc;
using Motor.Claim.Application.Dtos.Auth;
using Motor.Claim.Application.Services;

using Microsoft.AspNetCore.Authorization;
using Motor.Claim.Domain.Enums;

namespace Motor.Claim.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;

        public AuthController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
        {
            try
            {
                var user = await _userService.RegisterAsync(request);

                return Ok(new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.IdType,
                    user.NRIC,
                    user.PassportNo,
                    user.IssueCountry,
                    user.MobileCountry,
                    user.MobileNumber,
                    user.IsMaybankGroupEmployee
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("create-user")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserWithRoleRequest request)
        {
            try
            {
                if (request.Role == UserRole.Customer)
                {
                    throw new ArgumentException("Use /api/auth/register for customer registration.");
                }

                var user = await _userService.RegisterAsync(request, request.Role, request.WorkshopId);

                return Ok(new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    user.Role,
                    user.IdType,
                    user.NRIC,
                    user.PassportNo,
                    user.IssueCountry,
                    user.MobileCountry,
                    user.MobileNumber,
                    user.IsMaybankGroupEmployee,
                    user.WorkshopId
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _userService.LoginAsync(request);

            if (result == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            return Ok(result);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            await _userService.RequestPasswordResetAsync(request);

            return Ok(new
            {
                message = "If the email exists, a password reset link has been sent."
            });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var success = await _userService.ResetPasswordAsync(request);

                if (!success)
                {
                    return BadRequest("Invalid or expired reset token.");
                }

                return Ok(new
                {
                    message = "Password has been reset successfully."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized("Invalid or missing UserId claim.");
            }

            var profile = await _userService.GetProfileAsync(userId);

            return profile == null ? NotFound() : Ok(profile);
        }

        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateMyProfileRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;

                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized("Invalid or missing UserId claim.");
                }

                var profile = await _userService.UpdateProfileAsync(userId, request);
                return Ok(profile);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
