# ADR-001: Arquitectura por capas pragmática

- **Estado:** Aceptado para el diseño inicial.
- **Fecha:** 2026-11-05.

## Contexto

NexusPay debe mostrar separación de responsabilidades y seguir siendo comprensible para una aplicación de portafolio individual. Un diseño con muchos servicios, buses y bibliotecas de mediación aumentaría el costo sin resolver una necesidad actual.

## Decisión

Usar cuatro proyectos (`Domain`, `Application`, `Infrastructure`, `Api`) con dependencias de código hacia dentro: Application depende de Domain; Infrastructure depende de Application y Domain para implementar sus contratos; API consume Application. `Api/Program.cs` es el composition root y referencia Infrastructure solo para registrar sus implementaciones en Dependency Injection. Los controladores no acceden directamente a Infrastructure. Domain no depende de capas externas. El frontend React consume HTTP.

## Consecuencias

- Se puede probar dominio/casos de uso sin servidor SQL.
- SQL Server y simuladores quedan fuera de reglas de negocio.
- No se añade MediatR, AutoMapper ni repositorio genérico.
- Los mapeos explícitos requieren algo de código repetido, aceptable para este alcance.
- Si aparecen muchos casos de uso complejos o carga asíncrona real, se revisará esta decisión con evidencia.

