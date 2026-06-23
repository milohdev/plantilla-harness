# Sesión actual

## Feature en progreso
Ninguna. `infrastructure-setup` COMPLETADA (2026-06-22).

## Estado
Infraestructura base implementada y verificada end-to-end con Docker.

- Solución compila sin errores ni warnings (`dotnet build LogisticsSystem.slnx`).
- `docker compose up` levanta los 3 servicios (api, postgres:16, redis:7);
  postgres y redis healthy, api responde.
- Migración inicial `InitialCreate` aplicada al arranque; seed del admin OK.

### Verificaciones realizadas
- `GET /health` → 200 (liveness, sin dependencias).
- `GET /ready` → 200 con dependencias arriba; → 503 al parar Redis.
- `X-Correlation-Id`: se genera si falta, se conserva si viene en el request,
  y se propaga a los logs de request (`[CorrelationId]` en console + file sink).
- Swagger expone el security scheme Bearer (botón Authorize).
- Seed: `admin@logistics.com` existe una sola vez, rol `Admin`, hash de
  `PasswordHasher`, `CreatedBy = "system"` (interceptor de auditoría funcionando).

### Pendiente de verificación runtime (diferido, sin endpoints aún)
- Rate limit 429 (política `"fixed"`, 60/min/IP) — aplicada a controllers via
  `MapControllers().RequireRateLimiting("fixed")`.
- Idempotencia replay (POST/PUT con `X-Idempotency-Key`, Redis TTL 24h).
Ambos quedan cableados; se verificarán al implementar la primera feature de
negocio con endpoint.

## Stack / decisiones materializadas
- MediatR 14, FluentValidation 12, Serilog (console+file), Swashbuckle 10
  (Microsoft.OpenApi 2.0 → namespace `Microsoft.OpenApi`, sin `.Models`).
- EF Core 10 + Npgsql; query filter global `ISoftDeletable`; interceptor de
  auditoría `IAuditable`; `PlaceholderCurrentUserProvider` devuelve `"system"`.
- `ValidationBehavior` lanza `ValidationException` (la atrapa Problem Details);
  `Result<T>` solo para errores de negocio.

## Pendiente
Iniciar la primera feature de negocio del backlog.

## Notas
- El csproj de la API se llama `Logistics.API.csproj` (carpeta `Logistics.Api`),
  namespace `Logistics.Api`. Se mantuvo el nombre existente.
- Comandos EF: `--project Logistics.Infrastructure --startup-project Logistics.Api`.
- `.env` se crea desde `.env.example` (no versionar `.env`).
