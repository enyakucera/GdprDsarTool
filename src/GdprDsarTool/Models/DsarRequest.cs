namespace GdprDsarTool.Models;

public class DsarRequest
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    
    public string RequesterEmail { get; set; } = string.Empty;
    public string RequesterName { get; set; } = string.Empty;
    
    public RequestType RequestType { get; set; }
    public string? RequestMessage { get; set; }
    
    public RequestStatus Status { get; set; }
    
    public string? ResponsePdfUrl { get; set; }
    public string? ResponseNotes { get; set; }
    
    public DateTime SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Navigation
    public Company Company { get; set; } = null!;
}

public enum RequestType
{
    Access,
    Delete,
    Rectify
}

public enum RequestStatus
{
    Pending,
    InProgress,
    Completed,
    Rejected
}
