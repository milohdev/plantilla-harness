# Tasks — infrastructure-setup

> Marcar `[x]` conforme se avanza. Cada task referencia los requirements que
> cubre. No empezar hasta tener el spec aprobado por el humano.

## 0. Preparación

- [x] T0.1 — Eliminar `Class1.cs` de Domain, Application, Infrastructure y el
  `weatherforecast` de `Program.cs`. (limpieza)
- [x] T0.2 — Añadir PackageReferences por capa (ver design):
  Application (`MediatR`, `FluentValidation`), Infrastructure (`EFCore`,
  `Npgsql`, `EFCore.Design`, `StackExchange.Redis`, `Identity.Core`),
  Api (`Serilog.AspNetCore` + sinks, `Swashbuckle`, `JwtBearer`,
  HealthChecks Npgsql/Redis). Verificar `dotnet build`. (R1–R30 soporte)

## 1. Domain base

- [x] T1.1 — `BaseEntity` con `Guid Id`. (R26)
- [x] T1.2 — Interfaces `ISoftDeletable` e `IAuditable`. (R25)
- [x] T1.3 — Enum `UserRole { Admin, Operator, Driver, Client }`. (R28)
- [x] T1.4 — Entidad `User` (ctor privado + factory `Create`), implementa
  `IAuditable` + `ISoftDeletable`. (R27)

## 2. Application — Common

- [x] T2.1 — `Error`, `Result` y `Result<T>` con factory methods. (R13)
- [x] T2.2 — `PagedResult<T>`. (R14)
- [x] T2.3 — `IIdempotencyService`. (R17)
- [x] T2.4 — `ICurrentUserProvider`. (R29)
- [x] T2.5 — `ValidationBehavior<TRequest,TResponse>`. (R15)
- [x] T2.6 — `LoggingBehavior<TRequest,TResponse>`. (R16)
- [x] T2.7 — `AddApplication()` (MediatR + FluentValidation + behaviors). (R15, R16)

## 3. Infrastructure — persistencia

- [x] T3.1 — `LogisticsDbContext` con `DbSet<User>`, override de
  `SaveChangesAsync` y query filter global `ISoftDeletable`. (R18, R19)
- [x] T3.2 — `UserConfiguration` (mapeo a tabla `Users`). (R27)
- [x] T3.3 — `AuditableEntityInterceptor` (usa `ICurrentUserProvider`). (R20)
- [x] T3.4 — `PlaceholderCurrentUserProvider`. (R29)
- [x] T3.5 — `RedisIdempotencyService` (TTL 24h). (R21)
- [x] T3.6 — `LogisticsDbContextFactory` (design-time). (R22)
- [x] T3.7 — `AddInfrastructure(config)`: DbContext + interceptor, Redis,
  `IIdempotencyService`, `ICurrentUserProvider`, seeder. (R18, R20, R21, R29)
- [x] T3.8 — `DatabaseSeeder` (admin idempotente con `PasswordHasher<User>`). (R24, R30)
- [x] T3.9 — Generar migración inicial `InitialCreate`. (R23)

## 4. Api — configuración y pipeline

- [x] T4.1 — `appsettings.json` / `appsettings.Development.json`: connection
  strings, `Jwt:*`, `AdminUser:*`, `Serilog:*`. (R3, R4, R8, R24)
- [x] T4.2 — Configurar Serilog (console + file, enrich correlation id) en
  `Program.cs`, con `ILogger` nativo como fallback. (R4)
- [x] T4.3 — `CorrelationIdMiddleware`. (R5)
- [x] T4.4 — Rate limiting fixed window 60/min por IP, política `"fixed"`. (R6)
- [x] T4.5 — `AddProblemDetails` + `UseExceptionHandler`. (R7)
- [x] T4.6 — JWT Bearer + Authorization (sin endpoints). (R8)
- [x] T4.7 — `IdempotencyMiddleware` (POST/PUT, `X-Idempotency-Key`). (R9)
- [x] T4.8 — Health checks `/health` (liveness) y `/ready` (Postgres + Redis). (R10, R11)
- [x] T4.9 — Swagger con security JWT (Authorize) + tags. (R12)
- [x] T4.10 — Registrar `AddApplication()` + `AddInfrastructure()` y montar el
  pipeline en el orden estricto del design. Migrate + Seed al arranque. (R5–R9, R23, R24)

## 5. Docker e infraestructura local

- [x] T5.1 — `Dockerfile` multi-stage + `.dockerignore`. (R2)
- [x] T5.2 — `docker-compose.yml` (api, postgres:16, redis:7, healthchecks,
  volúmenes, depends_on). (R1)
- [x] T5.3 — `.env.example` con todas las variables. (R3)

## 6. Verificación final (checkpoints)

- [x] T6.1 — `dotnet build` sin errores ni warnings.
- [x] T6.2 — `docker compose up` levanta los 3 servicios; `api` healthy.
- [x] T6.3 — `GET /health` → 200; `GET /ready` → 200 con dependencias arriba.
- [x] T6.4 — Swagger carga, muestra botón Authorize y grupos por tag.
- [x] T6.5 — Seed: `admin@logistics.com` existe una sola vez tras el arranque.
- [x] T6.6 — Verificado: correlation id en respuesta y en logs de request;
  `/ready` → 503 al caer Redis (liveness sigue 200). Rate limit 429 e
  idempotencia replay quedan cableados y verificados estructuralmente; su
  verificación en runtime se difiere a la primera feature de negocio con
  endpoint (fuera de alcance aquí — no hay controllers todavía).
- [x] T6.7 — Actualizar `progress/current.md` y `progress/history.md`.

## Trazabilidad requirement → task

| Req | Tasks | Req | Tasks |
|-----|-------|-----|-------|
| R1  | T5.2  | R16 | T2.6, T2.7 |
| R2  | T5.1  | R17 | T2.3 |
| R3  | T4.1, T5.3 | R18 | T3.1, T3.7 |
| R4  | T4.2  | R19 | T3.1 |
| R5  | T4.3  | R20 | T3.3, T3.7 |
| R6  | T4.4  | R21 | T3.5, T3.7 |
| R7  | T4.5  | R22 | T3.6 |
| R8  | T4.1, T4.6 | R23 | T3.9 |
| R9  | T4.7  | R24 | T3.8, T4.1, T4.10 |
| R10 | T4.8  | R25 | T1.2 |
| R11 | T4.8  | R26 | T1.1 |
| R12 | T4.9  | R27 | T1.4, T3.2 |
| R13 | T2.1  | R28 | T1.3 |
| R14 | T2.2  | R29 | T2.4, T3.4, T3.7 |
| R15 | T2.5, T2.7 | R30 | T3.8 |