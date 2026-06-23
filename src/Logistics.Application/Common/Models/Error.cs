namespace Logistics.Application.Common.Models;

/// <summary>
/// Error de negocio modelado como valor (no excepción).
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string message) => new("not_found", message);
    public static Error Conflict(string message) => new("conflict", message);
    public static Error Validation(string message) => new("validation", message);
    public static Error Unauthorized(string message) => new("unauthorized", message);
}
