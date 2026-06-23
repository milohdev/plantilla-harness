# Design — infrastructure-setup

## Contexto del estado actual

La solución `LogisticsSystem.slnx` ya existe con 4 proyectos en `src/`:

- `Logistics.Domain` — sin referencias (correcto).
- `Logistics.Application` → referencia Domain.
- `Logistics.Infrastructure` → referencia Application.
- `Logistics.Api` → referencia Application + Infrastructure.

Los proyectos están en estado plantilla: `Program.cs` contiene el
`weatherforecast` de ejemplo y cada capa tiene un `Class1.cs` placeholder.
Todos compilan en `net10.0` con `Nullable` e `ImplicitUsings` habilitados.

> Nota: el csproj de la API se llama `Logistics.API.csproj` (carpeta
> `Logistics.Api`). Se mantiene el nombre existente; el namespace raíz será
> `Logistics.Api`.

Trabajo de esta feature: eliminar los placeholders, añadir paquetes, y crear
la estructura descrita abajo respetando la regla de dependencia.

## Decisiones de diseño

| Tema | Decisión | Requirement |
|------|----------|-------------|
| Hashing | `PasswordHasher<User>` nativo | R24, R30 |
| Errores HTTP | Problem Details nativo (sin middleware custom) | R7 |
| Swagger | Swashbuckle | R12 |
| Usuario actual | `ICurrentUserProvider` placeholder (GUID/sistema fijo) | R20, R29 |
| Logging | Serilog (console+file); `ILogger` nativo como fallback | R4 |
| Rate limit store | Fixed window nativo en memoria, partición por IP | R6 |
| Idempotencia store | Redis (StackExchange.Redis), TTL 24h | R9, R21 |
| Soft delete | Query filter global por `ISoftDeletable` | R19 |
| Auditoría | `SaveChangesInterceptor` de EF Core | R20 |

## Estructura de archivos a crear

### Raíz del repo
```
docker-compose.yml          # R1
Dockerfile                  # R2 (multi-stage, contexto = src)
.env.example                # R3
.dockerignore               # soporte R2
```

### Logistics.Domain
```
Common/
  BaseEntity.cs             # R26
  ISoftDeletable.cs         # R25
  IAuditable.cs             # R25
Users/
  User.cs                   # R27 (ctor privado + factory Create)
  UserRole.cs               # R28 (enum)
```

### Logistics.Application
```
Common/
  Models/
    Result.cs               # R13 (Result y Result<T>)
    PagedResult.cs          # R14
    Error.cs                # tipo de error de negocio (code + message)
  Behaviors/
    ValidationBehavior.cs   # R15
    LoggingBehavior.cs      # R16
  Interfaces/
    IIdempotencyService.cs  # R17
    ICurrentUserProvider.cs # R29
  DependencyInjection.cs    # AddApplication(): MediatR + FluentValidation + behaviors
```

### Logistics.Infrastructure
```
Persistence/
  LogisticsDbContext.cs                 # R18, R19 (SaveChangesAsync + query filters)
  Configurations/
    UserConfiguration.cs                # mapeo de User → tabla Users
  Interceptors/
    AuditableEntityInterceptor.cs       # R20
  LogisticsDbContextFactory.cs          # R22 (IDesignTimeDbContextFactory)
  Seed/
    DatabaseSeeder.cs                   # R24 (admin idempotente)
  Migrations/
    <timestamp>_InitialCreate.cs        # R23
Idempotency/
  RedisIdempotencyService.cs            # R21
Identity/
  PlaceholderCurrentUserProvider.cs     # R29
DependencyInjection.cs                  # AddInfrastructure(config): DbContext, Redis, services, interceptor
```

### Logistics.Api
```
Program.cs                              # reescrito: Serilog, DI, pipeline, health, swagger
Middleware/
  CorrelationIdMiddleware.cs            # R5
  IdempotencyMiddleware.cs              # R9
Extensions/
  (opcional) ServiceCollectionExtensions para Swagger/JWT/RateLimiting/HealthChecks
appsettings.json                        # connection strings, JWT, Serilog, admin creds
appsettings.Development.json
```
Se eliminan los `Class1.cs` de cada capa y el `weatherforecast` de `Program.cs`.

## Detalle por componente

### R5 — CorrelationIdMiddleware
Constante `HeaderName = "X-Correlation-Id"`. Lee el header; si vacío genera
`Guid.NewGuid().ToString()`. Guarda en `HttpContext.Items` y en
`context.Response.OnStarting` añade el header de respuesta. Abre un
`LogContext.PushProperty("CorrelationId", id)` de Serilog para que todos los
logs del request lo lleven.

### R6 — Rate limiting
`builder.Services.AddRateLimiter(...)` con `AddFixedWindowLimiter("fixed", o => {
PermitLimit = 60; Window = 1 min; QueueLimit = 0; })` particionado por
`HttpContext.Connection.RemoteIpAddress`. `RejectionStatusCode = 429`.
`app.UseRateLimiter()`. Los controllers usarán `[EnableRateLimiting("fixed")]`
(ya referenciado en conventions.md).

### R7 — Problem Details
`builder.Services.AddProblemDetails()` (con customización del
`CorrelationId` en `extensions`). `app.UseExceptionHandler()`. Sin clases
custom. Errores de negocio NO pasan por aquí: se devuelven como `Result`.

### R8 — JWT
`AddAuthentication(JwtBearerDefaults).AddJwtBearer(...)` con
`TokenValidationParameters` (ValidateIssuer/Audience/Lifetime/IssuerSigningKey)
leídos de `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`. `AddAuthorization()`.
`app.UseAuthentication(); app.UseAuthorization();`. Sin endpoints de login.

### R9 + R21 — Idempotencia
`IIdempotencyService` (Application):
```csharp
Task<string?> GetCachedResponseAsync(string key, CancellationToken ct);
Task SaveResponseAsync(string key, string response, TimeSpan ttl, CancellationToken ct);
```
`IdempotencyMiddleware` (Api): si método ∈ {POST, PUT} y existe header
`X-Idempotency-Key`: consulta el servicio; si hay hit, escribe la respuesta
cacheada (status + body) y corta; si no, buffer del `Response.Body`, ejecuta
`next`, y al terminar serializa y guarda con TTL 24h.
`RedisIdempotencyService` (Infrastructure): clave `idemp:{key}`,
`StringSetAsync`/`StringGetAsync`, `expiry = 24h`.

### R10/R11 — Health checks
`AddHealthChecks().AddNpgSql(conn).AddRedis(conn)`. Mapeos:
- `/health` → `MapHealthChecks` con `Predicate = _ => false` (solo liveness).
- `/ready` → `MapHealthChecks` que incluye los checks de Npgsql + Redis.

### R12 — Swagger
`AddSwaggerGen` con `AddSecurityDefinition("Bearer", ...)` +
`AddSecurityRequirement`. Tags por controller (`[Tags("...")]` o convención).
`UseSwagger()/UseSwaggerUI()` en Development.

### R13/R14 — Result y PagedResult
```csharp
public class Result {
  public bool IsSuccess { get; }
  public Error? Error { get; }
  public static Result Success();
  public static Result Failure(Error error);
}
public sealed class Result<T> : Result {
  public T? Value { get; }
  public static Result<T> Success(T value);
  public static new Result<T> Failure(Error error);
}
public sealed class PagedResult<T> {
  IReadOnlyList<T> Items; int TotalCount; int Page; int PageSize;
  int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

### R15/R16 — Behaviors MediatR
`ValidationBehavior`: resuelve `IEnumerable<IValidator<TRequest>>`, valida en
paralelo, junta `ValidationFailure`s; si hay errores lanza
`ValidationException` (la atrapa Problem Details) — alternativa: mapear a
`Result` si `TResponse` es `Result`. Decisión: lanzar `ValidationException`
para input inválido (no es error de negocio).
`LoggingBehavior`: `Stopwatch`, log de inicio/fin con nombre de request y ms.

### R18/R19 — DbContext
`LogisticsDbContext : DbContext`. `DbSet<User> Users`.
`OnModelCreating`: aplica `IEntityTypeConfiguration` del assembly y, para cada
entidad que implemente `ISoftDeletable`, añade query filter
`e => !e.IsDeleted` (vía reflexión sobre `Model.GetEntityTypes()`).
`SaveChangesAsync` override: punto de extensión (los timestamps los pone el
interceptor; el override queda para soft-delete futuro / dispatch de eventos).

### R20 — AuditableEntityInterceptor
`SaveChangesInterceptor.SavingChangesAsync`: recorre
`ChangeTracker.Entries<IAuditable>()`; `Added` → `CreatedAt = now`,
`CreatedBy = currentUser`; `Modified` → `UpdatedAt = now`,
`UpdatedBy = currentUser`. `now = DateTime.UtcNow`. Usuario desde
`ICurrentUserProvider`. Registrado con `AddInterceptors` en `AddDbContext`.

### R22/R23/R24 — Migraciones y seed
`LogisticsDbContextFactory : IDesignTimeDbContextFactory<LogisticsDbContext>`
lee la connection string de `appsettings.json`/env para `dotnet ef`.
Migración inicial `InitialCreate`. `DatabaseSeeder.SeedAsync`: si no existe
`admin@logistics.com`, crea `User` admin con `PasswordHasher<User>` sobre
`Admin123!`. Se invoca en el arranque (`app.Services.CreateScope()` →
`Migrate()` + `SeedAsync()`), idempotente.

### R29 — ICurrentUserProvider
```csharp
public interface ICurrentUserProvider { string UserId { get; } }
```
Placeholder devuelve un identificador fijo de sistema (p.ej. "system") hasta
que exista auth real. Documentado como temporal.

## Orden del pipeline en Program.cs (R5–R9)
```
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();   // R5
app.UseRateLimiter();                           // R6
app.UseExceptionHandler();                      // R7
app.UseAuthentication();                        // R8
app.UseAuthorization();                         // R8
app.UseMiddleware<IdempotencyMiddleware>();     // R9
// health, swagger, controllers
```

## Configuración (appsettings + .env.example)
- `ConnectionStrings:Postgres`, `ConnectionStrings:Redis`
- `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`
- `AdminUser:Email`, `AdminUser:Password` (Admin123! por defecto)
- `Serilog:*` (niveles y file path)

`.env.example` mapea estas a variables que `docker-compose.yml` inyecta
(`POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_DB`, `JWT_KEY`, etc.).

## Docker (R1/R2)
- `Dockerfile` multi-stage: `mcr.microsoft.com/dotnet/sdk:10.0` (restore →
  publish) y `mcr.microsoft.com/dotnet/aspnet:10.0` (runtime). Expone 8080.
- `docker-compose.yml`: `postgres:16`, `redis:7`, `api` (build local).
  Healthchecks: `pg_isready` y `redis-cli ping`. `api.depends_on` con
  `condition: service_healthy`. Volúmenes `pgdata` y `redisdata`.

## Riesgos / notas
- El csproj de API se llama `Logistics.API.csproj`; comandos `dotnet ef`
  deben apuntar al `Logistics.Infrastructure` con startup project `Logistics.Api`.
- Capturar el body en `IdempotencyMiddleware` requiere `EnableBuffering`/swap
  del `Response.Body` con un `MemoryStream`; cuidar restaurarlo siempre.
- Serilog file sink: asegurar que el path es escribible dentro del contenedor.