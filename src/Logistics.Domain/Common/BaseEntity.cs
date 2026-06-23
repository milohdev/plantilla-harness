namespace Logistics.Domain.Common;

/// <summary>
/// Clase base para todas las entidades del dominio. Provee la identidad (Guid).
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
