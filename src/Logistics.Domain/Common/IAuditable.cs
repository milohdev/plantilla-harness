namespace Logistics.Domain.Common;

/// <summary>
/// Entidades cuyos campos de auditoría rellena automáticamente el interceptor
/// de EF Core al insertar/actualizar.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
}
