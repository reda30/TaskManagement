using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Seeding;

public class DatabaseSeeder
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(AppDbContext context, IPasswordHasher passwordHasher, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Checking for pending migrations...");
            var pending = (await _context.Database.GetPendingMigrationsAsync()).ToList();
            _logger.LogInformation("{Count} pending migration(s) found.", pending.Count);

            await _context.Database.MigrateAsync();
            _logger.LogInformation("Migrations applied successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MigrateAsync failed — falling back to EnsureCreated.");
            await _context.Database.EnsureCreatedAsync();
        }

        var adminExists = await _context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == "admin@example.com");

        if (!adminExists)
        {
            var hash = _passwordHasher.Hash("Admin@123");
            var admin = User.Create("Administrator", "admin@example.com", hash, "Admin");
            await _context.Users.AddAsync(admin);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Admin user seeded successfully.");
        }
        else
        {
            _logger.LogInformation("Admin user already exists — skipping seed.");
        }
    }
}
