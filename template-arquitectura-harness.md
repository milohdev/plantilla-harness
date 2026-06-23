# Contexto Base — Clean Architecture .NET + Harness SDD

## Qué es esto
Template reutilizable para cualquier proyecto backend .NET con Clean Architecture. Incluye la arquitectura, el harness de trabajo con Claude Code, y las convenciones. Pegar al inicio de un chat nuevo para que Claude tenga el contexto completo.

---

## Flujo de trabajo (Harness Engineering + SDD)

Uso Claude Code (Sonnet, effort auto) para implementar, guiado por un harness de archivos markdown en el repo.

**Flujo por feature:**
1. Le digo a Claude Code qué feature quiero
2. Él genera el spec (requirements.md + design.md + tasks.md) en specs/<feature>/
3. PARA y pide aprobación humana
4. Yo reviso y apruebo (o pido cambios)
5. Él implementa task por task
6. Actualiza progress/current.md
7. Siguiente feature

---

## Stack tecnológico

- .NET 10
- PostgreSQL 16
- Redis 7 (idempotency keys)
- EF Core (ORM)
- MediatR (CQRS)
- FluentValidation
- Serilog (structured logging)
- Docker + Docker Compose (obligatorio)
- Swashbuckle (Swagger con JWT)

---

## Arquitectura — Clean Architecture, 4 capas

### Domain (Proyecto.Domain)
No depende de nada. Es el centro.
- Entidades con constructor privado + factory methods
- Value Objects
- Enums
- Interfaces de repositorio
- Domain Events
- Interfaces: ISoftDeletable, IAuditable
- Clase base: BaseEntity (Guid Id)

### Application (Proyecto.Application)
Depende solo de Domain.
- Commands y Queries (CQRS via MediatR)
- Un handler por command/query
- DTOs de entrada y salida
- Interfaces de servicios externos
- Pipeline Behaviors: ValidationBehavior, LoggingBehavior
- Common: Result<T>, PagedResult<T>

### Infrastructure (Proyecto.Infrastructure)
Depende de Application.
- DbContext (EF Core)
- Repositorios concretos
- Servicios externos
- Configurations de EF Core
- Migrations
- RedisIdempotencyService

### Api (Proyecto.Api)
Depende de Application + Infrastructure.
- Controllers (solo despachan via MediatR, cero lógica)
- Program.cs (DI, middleware, auth, rate limiting)
- Middleware: CorrelationIdMiddleware, IdempotencyMiddleware
- Configuración: JWT, Swagger, Serilog

### Regla de dependencia
Domain ← Application ← Infrastructure ← Api
Domain no conoce a nadie. Las capas externas dependen de las internas, nunca al revés.

---

## Patrones de diseño

- **CQRS con MediatR:** Commands (escritura) separados de Queries (lectura)
- **Repository Pattern:** interfaces en Domain, implementaciones en Infrastructure
- **Result Pattern:** errores de negocio como valores, no excepciones
- **Domain Events:** entidades emiten eventos, handlers separados los procesan
- **State Pattern:** máquina de estados con transiciones validadas (cuando aplique)
- **Pipeline Behaviors:** validación y logging como cross-cutting concerns
- **Specification Pattern:** filtros complejos encapsulados para queries
- **Idempotency Pattern:** via Redis, previene requests duplicados en POST/PUT
- **Soft Delete:** ISoftDeletable + query filter global en EF Core

---

## Pipeline del request

```
Request HTTP
  → Serilog (loguea request)
  → CorrelationIdMiddleware (asigna/lee X-Correlation-Id)
  → Rate Limiter (nativo ASP.NET Core, fixed window por IP)
  → Problem Details nativo (atrapa excepciones → RFC 9457)
  → Authentication (JWT Bearer)
  → Authorization (roles)
  → IdempotencyMiddleware (verifica duplicados en Redis, solo POST/PUT)
  → Controller → MediatR → ValidationBehavior → LoggingBehavior → Handler
```

---

## Decisiones técnicas fijas

- Problem Details nativo (AddProblemDetails + UseExceptionHandler), NO middleware custom
- ValidationBehavior lanza ValidationException para input inválido (la atrapa Problem Details)
- Result<T> solo para errores de negocio
- PasswordHasher<T> nativo de ASP.NET Core
- Serilog console + file sink
- Rate Limiting nativo ASP.NET Core (sin nginx, sin paquetes extra)
- Idempotency con Redis (StackExchange.Redis)
- NO tests en flujo principal (si sobra tiempo, 1-2 unit tests al final)

---

## Estructura del harness

```
Proyecto/
├── CLAUDE.md                    
├── CHECKPOINTS.md               
├── docs/
│   ├── architecture.md          
│   └── conventions.md           
├── specs/                       ← vacía, se llena feature por feature
│   └── <feature-name>/
│       ├── requirements.md
│       ├── design.md
│       └── tasks.md
├── progress/
│   ├── current.md
│   └── history.md
├── src/
│   ├── Proyecto.Api/
│   ├── Proyecto.Application/
│   ├── Proyecto.Domain/
│   └── Proyecto.Infrastructure/
├── docker-compose.yml
├── Dockerfile
├── .env.example
└── Proyecto.slnx
```

---

## Contenido del CLAUDE.md

```markdown
# Instrucciones para Claude Code

Tu rol: desarrollador backend .NET senior. Implementas features una a la vez siguiendo SDD.

Protocolo de arranque (cada sesión):
1. Lee progress/current.md
2. Lee docs/architecture.md y docs/conventions.md
3. Si hay feature en progreso, lee su spec en specs/<feature>/
4. Ejecuta dotnet build para verificar que compila

Reglas duras:
- Una sola feature a la vez
- No implementes sin spec aprobado
- No saltes la puerta de aprobación humana
- No declares tarea terminada sin que compile (dotnet build) y los endpoints respondan en Swagger
- Documenta lo que haces en progress/current.md mientras trabajas
- Controllers delgados: solo despachan via MediatR
- No expongas entidades de dominio. Solo DTOs
- Result pattern para errores de negocio, excepciones solo para errores inesperados
- NO generes proyectos ni código de tests salvo que el humano lo pida explícitamente

Flujo SDD:
1. Escribir spec (requirements.md + design.md + tasks.md)
2. PAUSA → pedir aprobación humana
3. Implementar task por task
4. dotnet build
5. Actualizar progress/current.md
```

---

## Contenido del CHECKPOINTS.md

```markdown
## Build y Verificación
- [ ] dotnet build sin errores ni warnings
- [ ] docker compose up levanta los 3 servicios
- [ ] Endpoints responden correctamente en Swagger
- [ ] Seed data se carga al iniciar

## Arquitectura
- [ ] Controllers solo despachan via MediatR, cero lógica
- [ ] Endpoints reciben y devuelven DTOs, nunca entidades de dominio
- [ ] Interfaces en Application, implementaciones en Infrastructure
- [ ] Domain no referencia ningún otro proyecto

## Código
- [ ] No hay TODOs sin contexto
- [ ] No hay código comentado
- [ ] No hay Console.WriteLine
- [ ] Validación via FluentValidation en Pipeline Behavior
- [ ] Errores de negocio retornan Result, no excepciones

## Documentación
- [ ] tasks.md tiene todas las tasks marcadas [x]
- [ ] progress/current.md actualizado
- [ ] Swagger documenta los endpoints de la feature
```

---

## Convenciones de código

### Naming
- Clases, métodos, propiedades: PascalCase
- Variables locales, parámetros: camelCase
- Interfaces: prefijo I (IShipmentRepository)
- Commands: verbo + sustantivo + Command (CreateShipmentCommand)
- Queries: Get + sustantivo + Query (GetShipmentsQuery)
- Handlers: mismo nombre + Handler (CreateShipmentHandler)
- DTOs: sustantivo + Dto (ShipmentDto)
- Validators: mismo nombre + Validator (CreateShipmentCommandValidator)
- Domain Events: sustantivo + pasado + Event (ShipmentCreatedEvent)

### Estructura de carpetas en Application
```
Application/
├── Common/
│   ├── Behaviors/          # ValidationBehavior, LoggingBehavior
│   ├── Interfaces/         # INotificationService, IIdempotencyService
│   └── Models/             # Result<T>, PagedResult<T>
├── [Feature]/
│   ├── Commands/
│   │   └── Create[Feature]/
│   │       ├── Create[Feature]Command.cs
│   │       ├── Create[Feature]Handler.cs
│   │       └── Create[Feature]Validator.cs
│   ├── Queries/
│   │   └── Get[Feature]s/
│   │       ├── Get[Feature]sQuery.cs
│   │       ├── Get[Feature]sHandler.cs
│   │       └── [Feature]Dto.cs
│   └── Events/
│       └── [Feature]CreatedEventHandler.cs
```

### Reglas generales
- Un archivo por clase
- Métodos async con sufijo Async en repos e infra (no en handlers)
- No usar regiones (#region)
- Inyección de dependencias solo por constructor
- Strings mágicos → constantes o enums
- Entidades validan su propio estado (constructor privado + factory methods)
- Soft delete: nunca DELETE físico en entidades críticas
- Idempotency: todo POST y PUT respeta X-Idempotency-Key
- Logs: siempre ILogger con Serilog, nunca Console.WriteLine
- Errores de negocio: Result<T>. Errores inesperados: Problem Details nativo

---

## Prompt de Feature 0 (Infrastructure Setup)

Este es el primer prompt que se le pega a Claude Code en cualquier proyecto nuevo, después de crear la solución y el harness:

```
Genera el spec para la feature "infrastructure-setup". Contexto:

Sistema [DESCRIBIR DOMINIO] en .NET 10 con Clean Architecture.
Lee docs/architecture.md y docs/conventions.md para el stack y convenciones.

Necesitamos la infraestructura base antes de features de negocio.

Alcance:

Docker Compose con 3 servicios:
- api (.NET 10)
- postgres (PostgreSQL 16)
- redis (Redis 7)
- Dockerfile multi-stage para la API
- archivo .env.example con las variables de entorno

Structured Logging:
- Serilog configurado en Program.cs (console + file sink)

Middleware pipeline (en este orden):
- CorrelationIdMiddleware: genera o lee header X-Correlation-Id
- Rate Limiting: nativo de ASP.NET Core, fixed window (60 req/min por IP)
- Problem Details nativo de .NET (AddProblemDetails + UseExceptionHandler), NO middleware custom
- Authentication/Authorization: JWT Bearer (configuración base, sin endpoints aún)
- IdempotencyMiddleware: solo para POST/PUT, lee header X-Idempotency-Key, 
  verifica en Redis, si existe retorna respuesta cacheada, 
  si no ejecuta y guarda resultado con TTL 24h

Health Checks:
- /health (liveness básico)
- /ready (verifica conexión a PostgreSQL + Redis)

Swagger:
- Configurado con autenticación JWT (botón Authorize)
- Agrupado por tags

Application Common:
- Result<T> con IsSuccess, Value, Error
- PagedResult<T> con Items, TotalCount, Page, PageSize
- ValidationBehavior<TRequest, TResponse> para MediatR
- LoggingBehavior<TRequest, TResponse> para MediatR
- IIdempotencyService (interface en Application)

Infrastructure:
- DbContext con override de SaveChangesAsync
- Query filter global para ISoftDeletable
- Interceptor de auditoría para IAuditable (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
- RedisIdempotencyService
- IDesignTimeDbContextFactory para migrations
- Migration inicial
- Seed: usuario Admin por defecto

Domain base:
- Interfaces: ISoftDeletable, IAuditable
- Clase base: BaseEntity (Guid Id)
- Entidad User (nombre, email, passwordHash, role, isActive)
- Enum UserRole: Admin, [OTROS ROLES DEL PROYECTO]

Decisiones ya tomadas:
- PasswordHasher<T> nativo de ASP.NET Core
- Credenciales del Admin fijas en configuración
- Swashbuckle para Swagger
- ICurrentUserProvider como placeholder hasta que exista auth real
- Problem Details nativo, no middleware custom

NO incluir: tests, outbox pattern, caching, testcontainers, 
middleware custom de excepciones, ni lógica de negocio más allá de User base.
```

---

## Creación de la solución (sin tests)

```bash
mkdir [NombreProyecto]
cd [NombreProyecto]
dotnet new sln
dotnet new webapi -n [Nombre].Api -o src/[Nombre].Api
dotnet new classlib -n [Nombre].Application -o src/[Nombre].Application
dotnet new classlib -n [Nombre].Domain -o src/[Nombre].Domain
dotnet new classlib -n [Nombre].Infrastructure -o src/[Nombre].Infrastructure

dotnet sln add src/[Nombre].Api
dotnet sln add src/[Nombre].Application
dotnet sln add src/[Nombre].Domain
dotnet sln add src/[Nombre].Infrastructure

dotnet add src/[Nombre].Api reference src/[Nombre].Application
dotnet add src/[Nombre].Api reference src/[Nombre].Infrastructure
dotnet add src/[Nombre].Application reference src/[Nombre].Domain
dotnet add src/[Nombre].Infrastructure reference src/[Nombre].Application

dotnet build
```

Después crear las carpetas del harness (docs/, specs/, progress/) y llenar los 6 archivos markdown. Luego abrir Claude Code desde la raíz del proyecto.
