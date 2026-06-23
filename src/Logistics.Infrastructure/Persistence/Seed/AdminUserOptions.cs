namespace Logistics.Infrastructure.Persistence.Seed;

/// <summary>
/// Credenciales fijas del usuario Admin por defecto, bindeadas desde la sección
/// "AdminUser" de la configuración.
/// </summary>
public sealed class AdminUserOptions
{
    public const string SectionName = "AdminUser";

    public string Name { get; init; } = "Administrator";
    public string Email { get; init; } = "admin@logistics.com";
    public string Password { get; init; } = "Admin123!";
}
