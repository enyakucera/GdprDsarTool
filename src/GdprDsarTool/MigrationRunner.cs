using Microsoft.EntityFrameworkCore;
using GdprDsarTool.Data;

namespace GdprDsarTool;

/// <summary>
/// Standalone migration runner for deployment pipelines.
/// Usage: dotnet GdprDsarTool.dll --migrate
/// </summary>
public static class MigrationRunner
{
    public static async Task<int> RunMigrationsAsync(string[] args)
    {
        try
        {
            Console.WriteLine("=== Database Migration Runner ===");
            Console.WriteLine($"Starting at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.Error.WriteLine("ERROR: Connection string not found");
                return 1;
            }

            Console.WriteLine($"Connection string configured: {MaskConnectionString(connectionString)}");

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(
                connectionString,
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null
                )
            );

            await using var context = new AppDbContext(optionsBuilder.Options);

            Console.WriteLine("Checking database connection and ensuring database exists...");

            // Try to connect - if DB doesn't exist, this will prepare to create it
            var canConnect = await context.Database.CanConnectAsync();

            if (!canConnect)
            {
                Console.WriteLine("Database doesn't exist yet. Will be created during migration.");
            }
            else
            {
                Console.WriteLine("Database connection successful!");
            }
            
            Console.WriteLine("Checking for pending migrations...");
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            var pendingList = pendingMigrations.ToList();

            if (pendingList.Count == 0)
            {
                Console.WriteLine("No pending migrations found. Database is up to date.");
            }
            else
            {
                Console.WriteLine($"Found {pendingList.Count} pending migration(s):");
                foreach (var migration in pendingList)
                {
                    Console.WriteLine($"  - {migration}");
                }

                Console.WriteLine("Applying migrations...");
                await context.Database.MigrateAsync();
                Console.WriteLine("Migrations applied successfully!");
            }

            // Check if seeding is needed
            if (!await context.Companies.AnyAsync())
            {
                Console.WriteLine("Database is empty. Running seed...");
                await DbInitializer.SeedAsync(context, configuration);
                Console.WriteLine("Seed completed successfully!");
            }
            else
            {
                Console.WriteLine("Database already contains data. Skipping seed.");
            }

            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            Console.WriteLine($"\nTotal applied migrations: {appliedMigrations.Count()}");
            Console.WriteLine($"Completed at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine("=== Migration Successful ===");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"=== Migration Failed ===");
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            return 1;
        }
    }

    private static string MaskConnectionString(string connectionString)
    {
        var parts = connectionString.Split(';');
        var masked = new List<string>();
        
        foreach (var part in parts)
        {
            if (part.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
                part.Contains("Pwd", StringComparison.OrdinalIgnoreCase))
            {
                var key = part.Split('=')[0];
                masked.Add($"{key}=***");
            }
            else
            {
                masked.Add(part);
            }
        }
        
        return string.Join(";", masked);
    }
}
