using MailKit.Net.Smtp;
using MimeKit;

namespace GdprDsarTool.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendRequestConfirmationAsync(string toEmail, string requesterName, string requestId)
    {
        var subject = "GDPR Data Request Received";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Data Subject Access Request Confirmation</h2>
                <p>Dear {requesterName},</p>
                <p>We have received your GDPR data request. Your request ID is: <strong>{requestId}</strong></p>
                <p>We will process your request within 30 days as required by GDPR regulations.</p>
                <p>You will receive another email once your request has been processed.</p>
                <br>
                <p>Best regards,<br>Compliance Team</p>
            </body>
            </html>
        ";

        await SendEmailAsync(toEmail, subject, body);
    }

    public async Task SendAdminNotificationAsync(string requestId, string requesterEmail)
    {
        var adminEmail = _config["AppSettings:AdminEmail"] ?? "admin@democompany.com";
        var subject = "New GDPR Data Request";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>New Data Subject Access Request</h2>
                <p>A new GDPR request has been submitted:</p>
                <ul>
                    <li><strong>Request ID:</strong> {requestId}</li>
                    <li><strong>Requester Email:</strong> {requesterEmail}</li>
                </ul>
                <p>Please log in to the admin panel to review and process this request.</p>
            </body>
            </html>
        ";

        await SendEmailAsync(adminEmail, subject, body);
    }

    private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                "GDPR DSAR Tool",
                _config["AppSettings:FromEmail"] ?? "noreply@gdprdsar.com"
            ));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            message.Body = new TextPart("html")
            {
                Text = htmlBody
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _config["AppSettings:SmtpHost"],
                int.Parse(_config["AppSettings:SmtpPort"] ?? "587"),
                MailKit.Security.SecureSocketOptions.StartTls
            );

            await client.AuthenticateAsync(
                _config["AppSettings:SmtpUsername"],
                _config["AppSettings:SmtpPassword"]
            );

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"Email sent successfully to {toEmail}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {toEmail}");
            // Don't throw - email failure shouldn't break the app
        }
    }
}
