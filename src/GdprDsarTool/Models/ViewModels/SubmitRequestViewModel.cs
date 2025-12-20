using System.ComponentModel.DataAnnotations;

namespace GdprDsarTool.Models.ViewModels;

public class SubmitRequestViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Your Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(255, MinimumLength = 2)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a request type")]
    [Display(Name = "Request Type")]
    public RequestType RequestType { get; set; }

    [StringLength(2000)]
    [Display(Name = "Additional Information (Optional)")]
    public string? Message { get; set; }
}
