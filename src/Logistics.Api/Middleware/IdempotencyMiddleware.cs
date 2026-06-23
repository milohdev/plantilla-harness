using System.Text;
using System.Text.Json;
using Logistics.Application.Common.Interfaces;

namespace Logistics.Api.Middleware;

/// <summary>
/// Idempotencia para operaciones de escritura (POST/PUT). Si llega un
/// X-Idempotency-Key ya visto, devuelve la respuesta cacheada en Redis; si no,
/// ejecuta el request y cachea la respuesta con TTL de 24h.
/// </summary>
public sealed class IdempotencyMiddleware
{
    public const string HeaderName = "X-Idempotency-Key";
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(24);

    private readonly RequestDelegate _next;

    public IdempotencyMiddleware(RequestDelegate next)
        => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsWriteMethod(context.Request.Method)
            || !context.Request.Headers.TryGetValue(HeaderName, out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues))
        {
            await _next(context);
            return;
        }

        var idempotencyKey = keyValues.ToString();
        var service = context.RequestServices.GetRequiredService<IIdempotencyService>();

        var cached = await service.GetCachedResponseAsync(idempotencyKey, context.RequestAborted);
        if (cached is not null)
        {
            await WriteCachedResponseAsync(context, cached);
            return;
        }

        // Capturar el cuerpo de la respuesta sin perder el stream original.
        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            buffer.Position = 0;
            var bodyText = await new StreamReader(buffer).ReadToEndAsync(context.RequestAborted);

            // Solo cachear respuestas exitosas.
            if (context.Response.StatusCode is >= 200 and < 300)
            {
                var envelope = new CachedResponse(context.Response.StatusCode, context.Response.ContentType, bodyText);
                await service.SaveResponseAsync(idempotencyKey, JsonSerializer.Serialize(envelope), Ttl, context.RequestAborted);
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static async Task WriteCachedResponseAsync(HttpContext context, string cached)
    {
        var envelope = JsonSerializer.Deserialize<CachedResponse>(cached);
        if (envelope is null)
            return;

        context.Response.StatusCode = envelope.StatusCode;
        if (!string.IsNullOrEmpty(envelope.ContentType))
            context.Response.ContentType = envelope.ContentType;
        context.Response.Headers["X-Idempotency-Replayed"] = "true";

        await context.Response.WriteAsync(envelope.Body ?? string.Empty, Encoding.UTF8);
    }

    private static bool IsWriteMethod(string method)
        => HttpMethods.IsPost(method) || HttpMethods.IsPut(method);

    private sealed record CachedResponse(int StatusCode, string? ContentType, string? Body);
}
