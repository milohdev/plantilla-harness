using Logistics.Application.Common.Interfaces;
using StackExchange.Redis;

namespace Logistics.Infrastructure.Idempotency;

/// <summary>
/// Implementación de IIdempotencyService sobre Redis. Las respuestas se guardan
/// con la key prefijada y un TTL (24h lo fija el middleware).
/// </summary>
public sealed class RedisIdempotencyService : IIdempotencyService
{
    private const string KeyPrefix = "idemp:";
    private readonly IConnectionMultiplexer _redis;

    public RedisIdempotencyService(IConnectionMultiplexer redis)
        => _redis = redis;

    public async Task<string?> GetCachedResponseAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(BuildKey(idempotencyKey));
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public async Task SaveResponseAsync(string idempotencyKey, string serializedResponse, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        await db.StringSetAsync(BuildKey(idempotencyKey), serializedResponse, ttl);
    }

    private static string BuildKey(string idempotencyKey) => KeyPrefix + idempotencyKey;
}
