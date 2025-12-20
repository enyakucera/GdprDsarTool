using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GdprDsarTool.Data;
using GdprDsarTool.Models;
using GdprDsarTool.Models.ViewModels;
using GdprDsarTool.Services;

namespace GdprDsarTool.Controllers;

public class AdminController : Controller
{
    private readonly AppDbContext _context;
    private readonly IPdfService _pdfService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        AppDbContext context,
        IPdfService pdfService,
        ILogger<AdminController> logger)
    {
        _context = context;
        _pdfService = pdfService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login()
    {
        // If already logged in, redirect to dashboard
        if (HttpContext.Session.GetString("AdminUserId") != null)
        {
            return RedirectToAction(nameof(Dashboard));
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var adminUser = await _context.AdminUsers
            .FirstOrDefaultAsync(u => u.Email == model.Email);

        if (adminUser == null || !BCrypt.Net.BCrypt.Verify(model.Password, adminUser.PasswordHash))
        {
            ModelState.AddModelError("", "Invalid email or password");
            return View(model);
        }

        // Set session
        HttpContext.Session.SetString("AdminUserId", adminUser.Id.ToString());
        HttpContext.Session.SetString("AdminEmail", adminUser.Email);
        HttpContext.Session.SetString("CompanyId", adminUser.CompanyId.ToString());

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        if (!IsAuthenticated())
        {
            return RedirectToAction(nameof(Login));
        }

        var companyId = Guid.Parse(HttpContext.Session.GetString("CompanyId")!);

        var requests = await _context.DsarRequests
            .Where(r => r.CompanyId == companyId)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync();

        return View(requests);
    }

    [HttpGet]
    public async Task<IActionResult> RequestDetail(Guid id)
    {
        if (!IsAuthenticated())
        {
            return RedirectToAction(nameof(Login));
        }

        var companyId = Guid.Parse(HttpContext.Session.GetString("CompanyId")!);

        var request = await _context.DsarRequests
            .Include(r => r.Company)
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId);

        if (request == null)
        {
            return NotFound();
        }

        return View(request);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(Guid id, RequestStatus status)
    {
        if (!IsAuthenticated())
        {
            return Unauthorized();
        }

        var companyId = Guid.Parse(HttpContext.Session.GetString("CompanyId")!);

        var request = await _context.DsarRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId);

        if (request == null)
        {
            return NotFound();
        }

        request.Status = status;
        
        if (status == RequestStatus.Completed)
        {
            request.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(RequestDetail), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> GeneratePdf(Guid id)
    {
        if (!IsAuthenticated())
        {
            return Unauthorized();
        }

        var companyId = Guid.Parse(HttpContext.Session.GetString("CompanyId")!);

        var request = await _context.DsarRequests
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == companyId);

        if (request == null)
        {
            return NotFound();
        }

        try
        {
            var pdfUrl = await _pdfService.GenerateDsarResponsePdfAsync(request);
            
            request.ResponsePdfUrl = pdfUrl;
            request.Status = RequestStatus.InProgress;
            
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "PDF generated successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for request {RequestId}", id);
            TempData["ErrorMessage"] = "Failed to generate PDF. Please try again.";
        }

        return RedirectToAction(nameof(RequestDetail), new { id });
    }

    private bool IsAuthenticated()
    {
        return HttpContext.Session.GetString("AdminUserId") != null;
    }
}
