# Historial de sesiones

## 2026-06-22 — infrastructure-setup (COMPLETADA)

Infraestructura base del sistema (.NET 10, Clean Architecture). Spec en
`specs/infrastructure-setup/` (requirements R1–R30, design, tasks).

**Entregado:**
- Domain base: `BaseEntity`, `ISoftDeletable`, `IAuditable`, `User`, `UserRole`.
- Application Common: `Result`/`Result<T>`, `PagedResult<T>`, `Error`,
  `ValidationBehavior`, `LoggingBehavior`, `IIdempotencyService`,
  `ICurrentUserProvider`, `AddApplication()`.
- Infrastructure: `LogisticsDbContext` (override `SaveChangesAsync` + query
  filter global soft-delete), `AuditableEntityInterceptor`,
  `RedisIdempotencyService`, `LogisticsDbContextFactory`, `DatabaseSeeder`
  (admin idempotente con `PasswordHasher`), `PlaceholderCurrentUserProvider`,
  migración `InitialCreate`, `AddInfrastructure()`.
- Api: `Program.cs` con Serilog (console+file), pipeline en orden estricto
  (CorrelationId → RateLimiter → ExceptionHandler/ProblemDetails → AuthN →
  AuthZ → Idempotency), health checks `/health` y `/ready`, Swagger con JWT,
  `CorrelationIdMiddleware`, `IdempotencyMiddleware`, rate limiting fixed window.
- Docker: `Dockerfile` multi-stage, `docker-compose.yml` (api + postgres:16 +
  redis:7 con healthchecks), `.env.example`, `.dockerignore`.

**Verificado:** build limpio; `docker compose up` con 3 servicios; /health 200;
/ready 200 (503 al caer Redis); correlation id generado/propagado en logs;
Swagger Authorize; seed admin único con auditoría `CreatedBy=system`.

**Diferido (sin endpoints aún):** verificación runtime de rate limit 429 e
idempotencia replay — cableados, se validan con la primera feature de negocio.

**Notas técnicas:** Microsoft.OpenApi 2.0 cambió namespace (`Microsoft.OpenApi`)
y `AddSecurityRequirement` ahora recibe `Func<OpenApiDocument,...>`. MediatR 14,
FluentValidation 12. El startup project (Api) requiere `EFCore.Design` para `dotnet ef`.
