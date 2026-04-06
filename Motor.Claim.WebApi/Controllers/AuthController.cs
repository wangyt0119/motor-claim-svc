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

                var user = await _userService.RegisterAsync(request, request.Role);

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
    }
}
