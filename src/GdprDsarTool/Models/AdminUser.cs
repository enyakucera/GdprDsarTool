namespace GdprDsarTool.Models;

public class AdminUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation
    public Company Company { get; set; } = null!;
}
