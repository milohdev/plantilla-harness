namespace Logistics.Application.Common.Interfaces;

/// <summary>
/// Almacén de respuestas idempotentes. La implementación (Redis) vive en
/// Infrastructure. Usado por el IdempotencyMiddleware en operaciones de escritura.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>Devuelve la respuesta cacheada para la key, o null si no existe.</summary>
    Task<string?> GetCachedResponseAsync(string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>Guarda la respuesta serializada con un TTL.</summary>
    Task SaveResponseAsync(string idempotencyKey, string serializedResponse, TimeSpan ttl, CancellationToken cancellationToken = default);
}
