using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motor.Claim.Application.Dtos.Auth;
using Motor.Claim.Application.Services;
using Motor.Claim.Domain.Enums;

namespace Motor.Claim.WebApi.Controllers
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminUsersController : ControllerBase
    {
        private readonly UserService _userService;

        public AdminUsersController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserWithRoleRequest request)
        {
            try
            {
                if (request.Role == UserRole.Customer)
                {
                    throw new ArgumentException("Use /api/auth/register for customer registration.");
                }

                var user = await _userService.RegisterAsync(request, request.Role, request.WorkshopId);
                var result = await _userService.GetProfileAsync(user.UserId);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers([FromQuery] UserRole? role, [FromQuery] bool? isActive)
        {
            var result = await _userService.GetUsersAsync(role, isActive);
            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var result = await _userService.GetProfileAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserAccountRequest request)
        {
            try
            {
                var result = await _userService.UpdateUserAccountAsync(id, request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id:guid}/activate")]
        public async Task<IActionResult> ActivateUser(Guid id)
        {
            try
            {
                var result = await _userService.SetUserActiveStatusAsync(id, true);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{id:guid}/deactivate")]
        public async Task<IActionResult> DeactivateUser(Guid id)
        {
            try
            {
                var result = await _userService.SetUserActiveStatusAsync(id, false);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
