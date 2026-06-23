# Convenciones de código

## Naming

- Clases, métodos, propiedades: PascalCase
- Variables locales, parámetros: camelCase
- Interfaces: prefijo I (IShipmentRepository)
- Commands: verbo + sustantivo + Command (CreateShipmentCommand)
- Queries: Get + sustantivo + Query (GetShipmentsQuery)
- Handlers: mismo nombre + Handler (CreateShipmentHandler)
- DTOs: sustantivo + Dto (ShipmentDto)
- Validators: mismo nombre + Validator (CreateShipmentCommandValidator)
- Domain Events: sustantivo + pasado + Event (ShipmentCreatedEvent)
- Middleware: nombre + Middleware (CorrelationIdMiddleware)

## Estructura de carpetas en Application

Application/
├── Common/
│   ├── Behaviors/          # ValidationBehavior, LoggingBehavior
│   ├── Interfaces/         # INotificationService, IIdempotencyService
│   └── Models/             # Result<T>, PagedResult<T>
├── Shipments/
│   ├── Commands/
│   │   └── CreateShipment/
│   │       ├── CreateShipmentCommand.cs
│   │       ├── CreateShipmentHandler.cs
│   │       └── CreateShipmentValidator.cs
│   ├── Queries/
│   │   └── GetShipments/
│   │       ├── GetShipmentsQuery.cs
│   │       ├── GetShipmentsHandler.cs
│   │       └── ShipmentDto.cs
│   └── Events/
│       └── ShipmentCreatedEventHandler.cs
├── Drivers/
│   └── ...
└── ...

## Estructura de Controllers

Ejemplo:
[ApiController]
[Route("api/v1/[controller]")]
public class ShipmentsController : ControllerBase
{
private readonly IMediator _mediator;

    public ShipmentsController(IMediator mediator)
        => _mediator = mediator;

    [HttpPost]
    [EnableRateLimiting("fixed")]
    public async Task<IActionResult> Create(
        CreateShipmentCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value }, result.Value)
            : BadRequest(result.Error);
    }
}

## Reglas generales

- Un archivo por clase
- Métodos async con sufijo Async en repos e infra (no en handlers, convención MediatR)
- No usar regiones (#region)
- No dejar usings sin usar
- Inyección de dependencias solo por constructor
- Nunca new dentro de handlers para servicios, siempre inyectar
- Strings mágicos → constantes o enums
- Entidades validan su propio estado (constructor privado + factory methods)
- Soft delete: nunca DELETE físico en entidades críticas
- Idempotency: todo endpoint POST y PUT debe respetar X-Idempotency-Key
- Logs: nunca Console.WriteLine, siempre ILogger con Serilog
- Errores de negocio: retornar Result<T>, nunca lanzar excepciones
- Errores inesperados: los atrapa Problem Details nativo (UseExceptionHandler)
- Problem Details (RFC 9457) para todas las respuestas de error