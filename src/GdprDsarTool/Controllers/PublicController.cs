using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GdprDsarTool.Data;
using GdprDsarTool.Models;
using GdprDsarTool.Models.ViewModels;
using GdprDsarTool.Services;

namespace GdprDsarTool.Controllers;

public class PublicController : Controller
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<PublicController> _logger;

    public PublicController(
        AppDbContext context,
        IEmailService emailService,
        ILogger<PublicController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult SubmitRequest()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitRequest(SubmitRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            // Get first company (for prototype - single tenant)
            var company = await _context.Companies.FirstOrDefaultAsync();
            if (company == null)
            {
                ModelState.AddModelError("", "System error: No company found. Please contact support.");
                return View(model);
            }

            // Create DSAR request
            var request = new DsarRequest
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                RequesterEmail = model.Email,
                RequesterName = model.FullName,
                RequestType = model.RequestType,
                RequestMessage = model.Message,
                Status = RequestStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };

            _context.DsarRequests.Add(request);
            await _context.SaveChangesAsync();

            // Send emails
            try
            {
                await _emailService.SendRequestConfirmationAsync(
                    model.Email,
                    model.FullName,
                    request.Id.ToString()
                );

                await _emailService.SendAdminNotificationAsync(
                    request.Id.ToString(),
                    model.Email
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send emails for request {RequestId}", request.Id);
                // Continue - request is saved even if email fails
            }

            return RedirectToAction(nameof(Confirmation), new { id = request.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting DSAR request");
            ModelState.AddModelError("", "An error occurred while submitting your request. Please try again.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(Guid id)
    {
        var request = await _context.DsarRequests
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            return NotFound();
        }

        return View(request);
    }
}
