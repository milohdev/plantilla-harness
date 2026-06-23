using Logistics.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Logistics.Infrastructure.Persistence.Seed;

/// <summary>
/// Siembra datos iniciales. Idempotente: no duplica el usuario Admin si ya existe.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly LogisticsDbContext _context;
    private readonly AdminUserOptions _adminOptions;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        LogisticsDbContext context,
        IOptions<AdminUserOptions> adminOptions,
        IPasswordHasher<User> passwordHasher,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _adminOptions = adminOptions.Value;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedAdminUserAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        var email = _adminOptions.Email.Trim().ToLowerInvariant();

        var exists = await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);
        if (exists)
        {
            _logger.LogInformation("Usuario admin ya existe ({Email}); seed omitido.", email);
            return;
        }

        var admin = User.Create(
            name: _adminOptions.Name,
            email: email,
            passwordHash: "placeholder",
            role: UserRole.Admin,
            isActive: true);

        admin.SetPasswordHash(_passwordHasher.HashPassword(admin, _adminOptions.Password));

        _context.Users.Add(admin);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Usuario admin sembrado ({Email}).", email);
    }
}
