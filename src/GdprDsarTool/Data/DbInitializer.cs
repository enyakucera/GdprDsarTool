using GdprDsarTool.Models;
using Microsoft.EntityFrameworkCore;

namespace GdprDsarTool.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context, IConfiguration configuration)
    {
        if (await context.Companies.AnyAsync())
        {
            return; // DB has been seeded
        }

        // Create demo company
        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = configuration["AppSettings:CompanyName"] ?? "Demo Company Ltd.",
            Email = "contact@democompany.com",
            CreatedAt = DateTime.UtcNow
        };

        context.Companies.Add(company);

        // Create admin user
        var adminEmail = configuration["AppSettings:AdminEmail"] ?? "admin@democompany.com";
        var adminPassword = configuration["AppSettings:AdminPassword"] ?? "Admin123!";

        var adminUser = new AdminUser
        {
            Id = Guid.NewGuid(),
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            CompanyId = company.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.AdminUsers.Add(adminUser);

        // Create sample DSAR requests for demo
        var sampleRequests = new[]
        {
            new DsarRequest
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                RequesterEmail = "john.doe@example.com",
                RequesterName = "John Doe",
                RequestType = RequestType.Access,
                RequestMessage = "I would like to access all my personal data you have stored.",
                Status = RequestStatus.Pending,
                SubmittedAt = DateTime.UtcNow.AddDays(-2)
            },
            new DsarRequest
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                RequesterEmail = "jane.smith@example.com",
                RequesterName = "Jane Smith",
                RequestType = RequestType.Delete,
                RequestMessage = "Please delete all my personal information from your systems.",
                Status = RequestStatus.InProgress,
                SubmittedAt = DateTime.UtcNow.AddDays(-5)
            }
        };

        context.DsarRequests.AddRange(sampleRequests);

        await context.SaveChangesAsync();
    }
}
