namespace Motor.Claim.Application.Interfaces
{
    public interface IEmailNotificationService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
        Task<(bool Success, string Message)> SendDiagnosticAsync(string toEmail, string subject, string htmlBody);
    }
}
