# Arquitectura

## Stack

- .NET 10
- PostgreSQL (base de datos principal)
- Redis (idempotency keys, rate limiting store)
- EF Core (ORM)
- MediatR (CQRS)
- FluentValidation
- Serilog (structured logging)
- Docker + Docker Compose

## Infraestructura (Docker Compose)

3 servicios:
- api: aplicación .NET
- postgres: PostgreSQL 16
- redis: Redis 7 (idempotency + rate limiting)

## Clean Architecture — 4 capas

### Domain (Logistics.Domain)
El centro. No depende de nada.
- Entidades: Shipment, Driver, Vehicle, Route, User, Tariff, Incident, etc.
- Value Objects: Address, GpsCoordinates, Money, DateRange
- Enums: ShipmentStatus, IncidentType, UserRole, TariffType
- Interfaces de repositorio: IShipmentRepository, IDriverRepository, etc.
- Domain Events: ShipmentCreatedEvent, DeliveryCompletedEvent, etc.
- Interfaces: ISoftDeletable, IAuditable

### Application (Logistics.Application)
Casos de uso. Depende solo de Domain.
- Commands y Queries (CQRS via MediatR)
- Handlers: un handler por command/query
- DTOs de entrada y salida
- Interfaces de servicios externos: INotificationService, IAiValidationService, IFileStorageService
- Pipeline Behaviors: ValidationBehavior, LoggingBehavior
- Common: Result<T>, PagedResult<T>, IIdempotencyService

### Infrastructure (Logistics.Infrastructure)
Implementaciones concretas. Depende de Application.
- LogisticsDbContext (EF Core)
- Repositorios concretos
- Servicios externos: email, IA, file storage
- Configuraciones de EF Core (EntityTypeConfiguration)
- Migrations
- RedisIdempotencyService

### Api (Logistics.Api)
Punto de entrada HTTP. Depende de Application e Infrastructure.
- Controllers agrupados por dominio
- Program.cs: DI, middleware, auth, rate limiting
- Middleware: CorrelationIdMiddleware, IdempotencyMiddleware, Problem Details nativo (AddProblemDetails + UseExceptionHandler)
- Configuración: JWT, Swagger, CORS, Serilog

## Regla de dependencia

Domain ← Application ← Infrastructure
← Api

Domain no conoce a nadie. Las capas externas dependen de las internas, nunca al revés.

## Patrones en uso

- CQRS con MediatR: Commands separados de Queries
- Repository Pattern: interfaces en Domain, implementaciones en Infrastructure
- Result Pattern: errores de negocio como valores, no excepciones
- Domain Events: entidades emiten eventos, handlers los procesan
- State Pattern: máquina de estados para ShipmentStatus
- Pipeline Behaviors: validación y logging como cross-cutting concerns
- Specification Pattern: filtros complejos encapsulados para queries
- Unit of Work: via DbContext de EF Core
- Idempotency Pattern: via Redis, previene requests duplicados en operaciones de escritura
- Soft Delete: ISoftDeletable + query filter global en EF Core

## Cross-cutting concerns

- Structured logging: Serilog con correlation ID en cada log entry
- Correlation ID: middleware que genera X-Correlation-Id por request
- Problem Details nativo (atrapa errores → RFC 9457)
- Rate limiting: nativo de ASP.NET Core, fixed window por IP
- Health checks: /health (liveness) y /ready (readiness, verifica BD + Redis)
- Auditoría: interceptor de EF Core que registra cambios en entidades IAuditable

## Flujo de un request típico

HTTP Request
→ CorrelationIdMiddleware (genera/lee X-Correlation-Id)
→ RateLimiter (verifica límites)
→ Problem Details nativo (atrapa errores → RFC 9457)
→ Authentication / Authorization (JWT + roles)
→ IdempotencyMiddleware (verifica duplicados en Redis, solo POST/PUT)
→ Controller (despacha Command/Query via MediatR)
→ ValidationBehavior (FluentValidation)
→ LoggingBehavior (Serilog con correlation ID)
→ Handler (lógica de caso de uso)
→ Repository / DbContext
→ Response DTO