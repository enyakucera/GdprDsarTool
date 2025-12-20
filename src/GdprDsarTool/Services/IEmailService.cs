namespace GdprDsarTool.Services;

public interface IEmailService
{
    Task SendRequestConfirmationAsync(string toEmail, string requesterName, string requestId);
    Task SendAdminNotificationAsync(string requestId, string requesterEmail);
}
