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

            // Check applied migrations first
            Console.WriteLine("Checking migration history...");
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            var appliedList = appliedMigrations.ToList();
            Console.WriteLine($"Currently applied migrations: {appliedList.Count}");

            // List all available migrations in the assembly
            Console.WriteLine("Checking available migrations in assembly...");
            var allMigrations = context.Database.GetMigrations();
            var allMigrationsList = allMigrations.ToList();
            Console.WriteLine($"Total migrations found in assembly: {allMigrationsList.Count}");
            if (allMigrationsList.Count > 0)
            {
                Console.WriteLine("Available migrations:");
                foreach (var migration in allMigrationsList)
                {
                    Console.WriteLine($"  - {migration}");
                }
            }
            else
            {
                Console.Error.WriteLine("ERROR: No migrations found in assembly!");
                Console.Error.WriteLine("This means migration files are not included in the published output.");
                Console.Error.WriteLine("Check Dockerfile and ensure Migrations folder is copied.");
                return 1;
            }

            Console.WriteLine("Checking for pending migrations...");
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            var pendingList = pendingMigrations.ToList();

            if (appliedList.Count == 0 && pendingList.Count == 0)
            {
                Console.WriteLine("WARNING: No migrations found in history and no pending migrations!");
                Console.WriteLine("This might indicate a fresh database. Running EnsureCreated or Migrate...");
            }

            if (pendingList.Count == 0)
            {
                Console.WriteLine("No pending migrations found.");

                // Double-check: If no applied migrations exist, force migrate
                if (appliedList.Count == 0)
                {
                    Console.WriteLine("No applied migrations in history. Forcing migration to ensure schema...");
                    await context.Database.MigrateAsync();
                    Console.WriteLine("Migration completed!");
                }
                else
                {
                    Console.WriteLine("Database is up to date.");
                }
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

                // Verify tables exist after migration
                Console.WriteLine("Verifying database schema...");
                try
                {
                    // Try to query Companies table to verify it exists
                    var companiesExist = await context.Companies.AnyAsync();
                    Console.WriteLine("✓ Companies table exists and is queryable");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"ERROR: Database schema verification failed!");
                    Console.Error.WriteLine($"Tables do not exist after migration: {ex.Message}");
                    Console.Error.WriteLine("Attempting fallback: EnsureCreated...");

                    try
                    {
                        await context.Database.EnsureCreatedAsync();
                        Console.WriteLine("EnsureCreated completed successfully!");
                    }
                    catch (Exception ensureEx)
                    {
                        Console.Error.WriteLine($"EnsureCreated also failed: {ensureEx.Message}");
                        return 1;
                    }
                }

                // Check if seeding is needed - use safe check
                Console.WriteLine("Checking if seeding is needed...");
            bool needsSeed = false;
            try
            {
                needsSeed = !await context.Companies.AnyAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Could not check Companies table: {ex.Message}");
                Console.WriteLine("Assuming seed is needed...");
                needsSeed = true;
            }

            if (needsSeed)
            {
                Console.WriteLine("Database is empty. Running seed...");
                await DbInitializer.SeedAsync(context, configuration);
                Console.WriteLine("Seed completed successfully!");
            }
            else
            {
                Console.WriteLine("Database already contains data. Skipping seed.");
            }

            var finalAppliedMigrations = await context.Database.GetAppliedMigrationsAsync();
            var finalAppliedList = finalAppliedMigrations.ToList();
            Console.WriteLine($"\nFinal migration status:");
            Console.WriteLine($"Total applied migrations: {finalAppliedList.Count}");
            if (finalAppliedList.Count > 0)
            {
                Console.WriteLine("Applied migrations:");
                foreach (var migration in finalAppliedList)
                {
                    Console.WriteLine($"  ✓ {migration}");
                }
            }
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
