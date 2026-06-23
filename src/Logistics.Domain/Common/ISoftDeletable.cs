namespace Logistics.Domain.Common;

/// <summary>
/// Entidades que se eliminan lógicamente (nunca DELETE físico).
/// El query filter global excluye las marcadas como eliminadas.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
}
