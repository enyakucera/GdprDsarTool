namespace GdprDsarTool.Models;

public class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    // Navigation
    public List<DsarRequest> DsarRequests { get; set; } = new();
}
