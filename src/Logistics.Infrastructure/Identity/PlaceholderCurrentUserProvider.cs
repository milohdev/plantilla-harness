using Logistics.Application.Common.Interfaces;

namespace Logistics.Infrastructure.Identity;

/// <summary>
/// Implementación placeholder de ICurrentUserProvider. Devuelve un identificador
/// fijo de sistema hasta que exista autenticación real.
/// </summary>
public sealed class PlaceholderCurrentUserProvider : ICurrentUserProvider
{
    public string UserId => "system";
}
