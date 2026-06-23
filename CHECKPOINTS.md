# Checkpoints — Criterios de calidad

Antes de marcar cualquier feature como terminada, verificar todo lo siguiente.

## Build y Verificación
- [ ] dotnet build sin errores ni warnings
- [ ] docker compose up levanta los 3 servicios
- [ ] Endpoints responden correctamente en Swagger
- [ ] Seed data se carga al iniciar

## Arquitectura
- [ ] Controllers solo despachan via MediatR, cero lógica
- [ ] Endpoints reciben y devuelven DTOs, nunca entidades de dominio
- [ ] Interfaces definidas en Application, implementaciones en Infrastructure
- [ ] Domain no referencia ningún otro proyecto

## Código
- [ ] No hay TODOs sin contexto
- [ ] No hay código comentado
- [ ] No hay Console.WriteLine ni print de debug
- [ ] Validación de input via FluentValidation en Pipeline Behavior
- [ ] Errores de negocio retornan Result, no lanzan excepciones

## Documentación
- [ ] tasks.md tiene todas las tasks marcadas [x]
- [ ] progress/current.md actualizado
- [ ] Swagger documenta los endpoints de la feature