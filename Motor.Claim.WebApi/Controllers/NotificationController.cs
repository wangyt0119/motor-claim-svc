using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.WebApi.Models;

namespace Motor.Claim.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "OfficerOrAdmin")]
    public class NotificationController : ControllerBase
    {
        private readonly IEmailNotificationService _emailNotificationService;

        public NotificationController(IEmailNotificationService emailNotificationService)
        {
            _emailNotificationService = emailNotificationService;
        }

        [HttpPost("test-email")]
        public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ToEmail))
            {
                return BadRequest("ToEmail is required.");
            }

            var result = await _emailNotificationService.SendDiagnosticAsync(
                request.ToEmail,
                "Motor Claim System test email",
                """
                <div style="font-family: Arial, sans-serif; color: #1f2937; line-height: 1.6;">
                    <p>Hello,</p>
                    <p>This is a test email from the Motor Claim System notification service.</p>
                    <p>If you received this, the SMTP configuration is working.</p>
                    <p>Regards,<br />Motor Claim System</p>
                </div>
                """);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message
                });
            }

            return Ok(new
            {
                success = true,
                message = result.Message
            });
        }
    }
}
