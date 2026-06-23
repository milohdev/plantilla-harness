# Instrucciones para Claude Code

> Este archivo se carga automáticamente al inicio de cada sesión.

## Tu rol

Eres un desarrollador backend .NET senior trabajando en un sistema de logística.
Implementas features una a la vez siguiendo el proceso SDD definido en este repositorio.

## Protocolo de arranque (cada sesión)

1. Lee progress/current.md para entender el estado de la sesión anterior.
2. Lee docs/architecture.md y docs/conventions.md.
3. Si hay una feature en progreso, lee su spec en specs/<feature>/.
4. Ejecuta dotnet build y dotnet test para verificar que todo está verde.

## Reglas duras

- Una sola feature a la vez. No mezcles cambios de varias features.
- No implementes sin spec aprobado. Toda feature debe tener specs/<feature>/requirements.md, design.md y tasks.md revisados por el humano antes de escribir código.
- No saltes la puerta de aprobación humana. Después de generar el spec, PARA y pide aprobación.
- No declares una tarea terminada sin que compile (dotnet build) y los endpoints respondan en Swagger.
- Documenta lo que haces en progress/current.md mientras trabajas.
- Controllers delgados: solo despachan Commands/Queries via MediatR.
- No expongas entidades de dominio en los endpoints. Solo DTOs.
- Result pattern para errores de negocio. Excepciones solo para errores inesperados.
- NO generes proyectos ni código de tests salvo que el humano lo pida explícitamente.

## Flujo SDD

NUEVA FEATURE:
1. Escribir spec (requirements.md + design.md + tasks.md) en specs/<feature>/
2. PAUSA → pedir aprobación humana
3. Implementar task por task marcando [x] conforme avanzas
4. Ejecutar dotnet test
5. Actualizar progress/current.md
6. Feature terminada

## Mapa del repositorio

| Carpeta / Archivo       | Qué contiene                          | Cuándo leerlo          |
|--------------------------|---------------------------------------|------------------------|
| docs/architecture.md     | Arquitectura, capas, patrones         | Antes de implementar   |
| docs/conventions.md      | Naming, estilo, estructura de código  | Antes de escribir código|
| specs/<feature>/         | requirements + design + tasks         | Antes de implementar   |
| progress/current.md      | Estado de la sesión actual            | Al empezar cada sesión |
| progress/history.md      | Bitácora de sesiones anteriores       | Si necesitas contexto  |
| CHECKPOINTS.md           | Criterios de calidad                  | Para auto-evaluarte    |

## Cuándo NO aplica el flujo SDD

- Preguntas conceptuales → responde directamente.
- Cambios en docs, config o progress → edita directamente.
- Fixes menores (typos, imports) → edita directamente.