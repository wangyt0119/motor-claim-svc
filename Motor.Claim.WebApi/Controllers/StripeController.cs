using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Motor.Claim.WebApi.Configuration;
using Motor.Claim.WebApi.Services;
using Stripe;

namespace Motor.Claim.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StripeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly StripeConnectService _stripeConnectService;

        public StripeController(
            IConfiguration configuration,
            StripeConnectService stripeConnectService)
        {
            _configuration = configuration;
            _stripeConnectService = stripeConnectService;
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook()
        {
            var stripeOptions = _configuration.GetSection("Payments:Stripe").Get<StripeOptions>() ?? new StripeOptions();
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(stripeOptions.WebhookSecret))
            {
                return BadRequest("Stripe webhook secret is not configured.");
            }

            try
            {
                var signatureHeader = Request.Headers["Stripe-Signature"];
                var stripeEvent = EventUtility.ConstructEvent(json, signatureHeader, stripeOptions.WebhookSecret);

                if (string.Equals(stripeEvent.Type, "account.updated", StringComparison.OrdinalIgnoreCase))
                {
                    var account = stripeEvent.Data.Object as Account;
                    if (account != null)
                    {
                        await _stripeConnectService.HandleAccountUpdatedAsync(account);
                    }
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
