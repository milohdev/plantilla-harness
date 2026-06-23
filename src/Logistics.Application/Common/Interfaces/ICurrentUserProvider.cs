namespace Logistics.Application.Common.Interfaces;

/// <summary>
/// Provee el identificador del usuario actual. Placeholder hasta que exista
/// autenticación real; lo consume el interceptor de auditoría.
/// </summary>
public interface ICurrentUserProvider
{
    string UserId { get; }
}
