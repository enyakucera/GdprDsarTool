using Microsoft.EntityFrameworkCore;
using GdprDsarTool.Models;

namespace GdprDsarTool.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies { get; set; } = null!;
    public DbSet<DsarRequest> DsarRequests { get; set; } = null!;
    public DbSet<AdminUser> AdminUsers { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Company
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // DsarRequest
        modelBuilder.Entity<DsarRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
            entity.Property(e => e.RequesterEmail).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RequesterName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RequestType).IsRequired()
                .HasMaxLength(50)
                .HasConversion<string>();
            entity.Property(e => e.Status).IsRequired()
                .HasMaxLength(50)
                .HasConversion<string>()
                .HasDefaultValue(RequestStatus.Pending);
            entity.Property(e => e.SubmittedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.ResponsePdfUrl).HasMaxLength(500);

            entity.HasOne(e => e.Company)
                .WithMany(c => c.DsarRequests)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.CompanyId).HasDatabaseName("IX_DsarRequests_CompanyId");
            entity.HasIndex(e => e.Status).HasDatabaseName("IX_DsarRequests_Status");
            entity.HasIndex(e => e.SubmittedAt).HasDatabaseName("IX_DsarRequests_SubmittedAt");
        });

        // AdminUser
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasDefaultValueSql("NEWID()");
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("IX_AdminUsers_Email");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.Company)
                .WithMany()
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
