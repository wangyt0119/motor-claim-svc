using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Motor.Claim.Application.Interfaces;
using Motor.Claim.WebApi.Configuration;

namespace Motor.Claim.WebApi.Services
{
    public class SmtpEmailNotificationService : IEmailNotificationService
    {
        private readonly SmtpEmailOptions _options;
        private readonly ILogger<SmtpEmailNotificationService> _logger;

        public SmtpEmailNotificationService(
            IOptions<SmtpEmailOptions> options,
            ILogger<SmtpEmailNotificationService> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            await SendInternalAsync(toEmail, subject, htmlBody, swallowException: true);
        }

        public async Task<(bool Success, string Message)> SendDiagnosticAsync(string toEmail, string subject, string htmlBody)
        {
            return await SendInternalAsync(toEmail, subject, htmlBody, swallowException: false);
        }

        private async Task<(bool Success, string Message)> SendInternalAsync(
            string toEmail,
            string subject,
            string htmlBody,
            bool swallowException)
        {
            if (!IsConfigured())
            {
                const string message = "SMTP email settings are incomplete. Check Host, Username, Password, and FromEmail.";
                _logger.LogWarning("{Message} Skipping notification to {Email}.", message, toEmail);
                return (false, message);
            }

            if (string.IsNullOrWhiteSpace(toEmail))
            {
                const string message = "Recipient email is empty.";
                _logger.LogWarning("{Message} Skipping notification.", message);
                return (false, message);
            }

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_options.FromEmail, _options.FromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail.Trim());

                using var client = new SmtpClient(_options.Host, _options.Port)
                {
                    EnableSsl = _options.EnableSsl,
                    Credentials = new NetworkCredential(_options.Username, _options.Password),
                    Timeout = 5000
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("Notification email sent successfully to {Email}.", toEmail);
                return (true, "Email sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification email to {Email}.", toEmail);

                if (!swallowException)
                {
                    return (false, ex.Message);
                }
            }

            return (false, "Email sending failed. Check API logs for details.");
        }

        private bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(_options.Host)
                && !string.IsNullOrWhiteSpace(_options.Username)
                && !string.IsNullOrWhiteSpace(_options.Password)
                && !string.IsNullOrWhiteSpace(_options.FromEmail);
        }
    }
}
