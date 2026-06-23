# Requirements — infrastructure-setup

## Objetivo

Establecer la infraestructura base del sistema de logística (.NET 10, Clean
Architecture) antes de implementar features de negocio. Cubre orquestación
con Docker, logging estructurado, pipeline de middleware, health checks,
Swagger con JWT, primitivas comunes de Application, persistencia con EF Core
+ PostgreSQL, idempotencia con Redis y el dominio base (`User`).

## Fuera de alcance (explícito)

- Tests de cualquier tipo (unit, integration, testcontainers).
- Outbox pattern, caching de queries, middleware custom de excepciones.
- Endpoints de autenticación (login/refresh). Solo se configura JWT.
- Lógica de negocio más allá de la entidad `User` base.

## Requirements

### Orquestación y contenedores

- **R1** — Existe un `docker-compose.yml` con 3 servicios: `api` (.NET 10),
  `postgres` (PostgreSQL 16) y `redis` (Redis 7). Postgres y Redis exponen
  volúmenes persistentes y healthchecks; `api` depende de ambos (depends_on
  con condición de salud).
  *Verificación:* `docker compose up` levanta los 3 servicios y `api` queda
  healthy.

- **R2** — Existe un `Dockerfile` multi-stage para la API (stage de build con
  SDK .NET 10, stage de runtime con ASP.NET runtime). La imagen final no
  contiene el SDK.
  *Verificación:* `docker compose build api` produce imagen ejecutable.

- **R3** — Existe `.env.example` documentando todas las variables de entorno
  consumidas (cadena de conexión Postgres, Redis, JWT issuer/audience/key,
  credenciales del admin, configuración de Serilog). No contiene secretos
  reales.
  *Verificación:* Cada variable referenciada en compose/appsettings aparece en
  `.env.example`.

### Logging estructurado

- **R4** — Serilog está configurado en `Program.cs` con sinks de console y
  file, lectura de configuración desde `appsettings.json`, y enriquecimiento
  con el correlation id de cada request. El `ILogger` nativo de .NET queda
  como fallback documentado si Serilog falla al inicializar.
  *Verificación:* Al arrancar, los logs salen en consola y en archivo, e
  incluyen el `CorrelationId`.

### Pipeline de middleware (orden estricto)

El orden de registro debe ser exactamente:
CorrelationId → RateLimiter → ProblemDetails/ExceptionHandler →
Authentication → Authorization → Idempotency.

- **R5** — `CorrelationIdMiddleware` lee el header `X-Correlation-Id`; si no
  existe lo genera (GUID), lo adjunta al `HttpContext`, lo propaga en el header
  de respuesta y lo expone al scope de logging de Serilog.
  *Verificación:* Request sin header recibe `X-Correlation-Id` en la respuesta;
  request con header conserva el mismo valor.

- **R6** — Rate limiting nativo de ASP.NET Core, política fixed window de
  60 req/min particionada por IP del cliente, registrada como política `"fixed"`.
  Respuesta 429 al exceder.
  *Verificación:* Superar 60 req/min desde una IP devuelve 429.

- **R7** — Manejo de errores vía Problem Details nativo (`AddProblemDetails` +
  `UseExceptionHandler`), produciendo respuestas RFC 9457. **No** se crea
  middleware custom de excepciones.
  *Verificación:* Una excepción no controlada produce un `application/problem+json`.

- **R8** — Authentication/Authorization JWT Bearer configurado (validación de
  issuer, audience, lifetime y signing key desde configuración). Sin endpoints
  de auth todavía. Authorization habilitado para uso futuro de `[Authorize]`.
  *Verificación:* Un endpoint protegido de prueba responde 401 sin token válido
  (validado vía Swagger Authorize).

- **R9** — `IdempotencyMiddleware` actúa solo en `POST` y `PUT`. Lee el header
  `X-Idempotency-Key`; consulta Redis vía `IIdempotencyService`; si existe una
  respuesta cacheada la retorna; si no, ejecuta el request, captura la respuesta
  y la guarda en Redis con TTL de 24h. Requests sin la key pasan sin idempotencia.
  *Verificación:* Dos POST con la misma `X-Idempotency-Key` devuelven la misma
  respuesta sin re-ejecutar el handler.

### Health checks

- **R10** — Endpoint `/health` (liveness) responde 200 sin dependencias externas.
  *Verificación:* `GET /health` → 200.

- **R11** — Endpoint `/ready` (readiness) verifica conectividad a PostgreSQL y
  Redis; responde 200 solo si ambos están disponibles, 503 en caso contrario.
  *Verificación:* `GET /ready` → 200 con dependencias arriba; 503 si una cae.

### Documentación de API

- **R12** — Swagger (Swashbuckle) configurado con esquema de seguridad JWT
  Bearer (botón Authorize) y operaciones agrupadas por tags.
  *Verificación:* La UI de Swagger muestra el botón Authorize y los grupos por tag.

### Application — Common

- **R13** — `Result` y `Result<T>` con `IsSuccess`, `Value`, `Error` y factory
  methods (`Success`, `Failure`). Errores de negocio se modelan como valores.
  *Verificación:* Compila y es usado por `LoggingBehavior`/controllers de ejemplo.

- **R14** — `PagedResult<T>` con `Items`, `TotalCount`, `Page`, `PageSize`
  (y derivados como `TotalPages`).
  *Verificación:* Compila y serializa correctamente.

- **R15** — `ValidationBehavior<TRequest,TResponse>` para MediatR ejecuta los
  `IValidator<TRequest>` de FluentValidation registrados y agrega los errores
  antes de llegar al handler.
  *Verificación:* Un request inválido se corta en el behavior (validado al
  implementar la primera feature; aquí se verifica registro en DI).

- **R16** — `LoggingBehavior<TRequest,TResponse>` para MediatR registra inicio,
  fin y duración de cada request, con el correlation id en el scope.
  *Verificación:* Los logs muestran el nombre del request y duración.

- **R17** — `IIdempotencyService` definida en Application
  (`Common/Interfaces`): métodos para obtener respuesta cacheada y guardar
  respuesta con TTL. La implementación vive en Infrastructure.
  *Verificación:* Interface existe en Application y es consumida por el middleware.

### Infrastructure

- **R18** — `LogisticsDbContext` (EF Core, provider Npgsql) con override de
  `SaveChangesAsync`. Registrado en DI con la cadena de conexión de Postgres.
  *Verificación:* La app conecta y aplica migraciones.

- **R19** — Query filter global para `ISoftDeletable`
  (`Where(e => !e.IsDeleted)`) aplicado a todas las entidades soft-deletable.
  *Verificación:* Una entidad con `IsDeleted = true` no aparece en queries normales.

- **R20** — Interceptor de auditoría de EF Core: para entidades `IAuditable`
  asigna `CreatedAt`/`CreatedBy` al insertar y `UpdatedAt`/`UpdatedBy` al
  actualizar, tomando el usuario de `ICurrentUserProvider`.
  *Verificación:* Insertar/actualizar una entidad auditable rellena los campos.

- **R21** — `RedisIdempotencyService` implementa `IIdempotencyService` usando
  Redis (StackExchange.Redis), con serialización de la respuesta y TTL de 24h.
  *Verificación:* Guarda y recupera respuestas por idempotency key.

- **R22** — `IDesignTimeDbContextFactory<LogisticsDbContext>` para soportar
  `dotnet ef migrations` en tiempo de diseño.
  *Verificación:* `dotnet ef migrations add` funciona sin levantar la app.

- **R23** — Existe la migración inicial que crea el esquema (incluida tabla
  `Users`).
  *Verificación:* `dotnet ef database update` crea las tablas.

- **R24** — Seed de datos: usuario Admin por defecto
  (`admin@logistics.com` / `Admin123!`, rol `Admin`, activo), con password
  hasheado vía `PasswordHasher<User>`. Credenciales fijas en configuración.
  Idempotente (no duplica si ya existe).
  *Verificación:* Al iniciar, el usuario admin existe en la BD una sola vez.

### Domain — base

- **R25** — Interfaces `ISoftDeletable` (`bool IsDeleted`, `DateTime? DeletedAt`)
  e `IAuditable` (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`) en Domain.
  *Verificación:* Compila; Domain no referencia otros proyectos.

- **R26** — Clase base `BaseEntity` con `Guid Id`.
  *Verificación:* `User` hereda de `BaseEntity`.

- **R27** — Entidad `User` (nombre, email, passwordHash, role, isActive),
  implementa `IAuditable` e `ISoftDeletable`, con constructor privado +
  factory method (encapsula su estado).
  *Verificación:* La entidad existe y se mapea a la tabla `Users`.

- **R28** — Enum `UserRole` con valores `Admin`, `Operator`, `Driver`, `Client`.
  *Verificación:* Compila y se persiste el valor del admin.

### Cross-cutting

- **R29** — `ICurrentUserProvider` (interface en Application, implementación
  placeholder en Infrastructure) que devuelve un identificador de usuario
  hasta que exista auth real. Consumido por el interceptor de auditoría.
  *Verificación:* El interceptor obtiene un `CreatedBy`/`UpdatedBy` no nulo.

- **R30** — El hashing de contraseñas usa `PasswordHasher<User>` nativo de
  ASP.NET Core (no librerías externas).
  *Verificación:* El hash del admin se genera con `PasswordHasher`.

## Dependencias de paquetes esperadas (a confirmar en design)

- Api: `Serilog.AspNetCore`, sinks Console/File, `Swashbuckle.AspNetCore`,
  `Microsoft.AspNetCore.Authentication.JwtBearer`,
  `Microsoft.AspNetCore.RateLimiting` (nativo), `AspNetCore.HealthChecks.*`
  (Npgsql + Redis).
- Application: `MediatR`, `FluentValidation`.
- Infrastructure: `Microsoft.EntityFrameworkCore`,
  `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`,
  `StackExchange.Redis`, `Microsoft.Extensions.Identity.Core` (PasswordHasher).