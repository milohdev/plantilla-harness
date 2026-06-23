using Logistics.Domain.Common;

namespace Logistics.Domain.Users;

/// <summary>
/// Usuario del sistema. Encapsula su estado: constructor privado + factory.
/// </summary>
public class User : BaseEntity, IAuditable, ISoftDeletable
{
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }

    // Auditoría (IAuditable) — rellenada por el interceptor de EF Core.
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Soft delete (ISoftDeletable).
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Requerido por EF Core.
    private User()
    {
    }

    private User(string name, string email, string passwordHash, UserRole role, bool isActive)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        IsActive = isActive;
    }

    /// <summary>
    /// Crea un usuario. El hash de la contraseña se calcula fuera de la entidad
    /// (PasswordHasher) y se inyecta ya hasheado.
    /// </summary>
    public static User Create(string name, string email, string passwordHash, UserRole role, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre es obligatorio.", nameof(name));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email es obligatorio.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("El hash de contraseña es obligatorio.", nameof(passwordHash));

        return new User(name, email.Trim().ToLowerInvariant(), passwordHash, role, isActive);
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("El hash de contraseña es obligatorio.", nameof(passwordHash));
        PasswordHash = passwordHash;
    }

    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}
