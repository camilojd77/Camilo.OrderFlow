# ADR-0006 — Integrar módulos con un puerto en el consumidor y un adaptador en el host

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** llegue el segundo módulo y se implemente la primera integración, o se extraiga un módulo a
  servicio (el adaptador pasaría a llamar por HTTP o mensajería).

## Contexto

Hoy solo existe `Identity`, así que no hay ninguna integración entre módulos implementada. La documentación y el
código decían cosas distintas: la guía de arquitectura proponía un adaptador **en el host** hacia una fachada pública
del proveedor; el comentario del test de aislamiento (`CleanArchitectureTests.cs`) habla de una implementación en la
**Infrastructure del consumidor**.

Alternativas: adaptador en la Infrastructure del consumidor (obliga a esa Infrastructure a referenciar al proveedor,
y el test de aislamiento lo prohibiría); referencias directas entre módulos (rompe ADR-0002); eventos de integración
con outbox (más infraestructura de la que hoy hace falta).

## Decisión

Si el módulo A necesita algo del módulo B:

1. A define un **puerto** (interfaz) en `A.Application/Abstractions`, con tipos propios de A.
2. B expone una **fachada pública** pequeña y estable (la superficie que acepta ser llamada desde fuera).
3. El **host** (`Camilo.OrderFlow.WebApi`) implementa el puerto de A con un adaptador que llama a la fachada de B y lo
   registra en la composición.

Ningún proyecto de A referencia a B ni al revés.

## Consecuencias

- El aislamiento entre módulos se mantiene y el test lo sigue comprobando.
- Al extraer B a un servicio, solo cambia el adaptador del host (pasa a HTTP/mensajería); A no se toca.
- El comentario de `CleanArchitectureTests.cs:141-145` contradice esta decisión: su corrección queda como propuesta
  (es código de tests).
- No hay aún ejemplo ejecutable: la primera integración establece la referencia y actualiza `docs/architecture.md`.

## Referencias

`docs/architecture.md` § Comunicación entre módulos, `Camilo.OrderFlow.WebApi/Program.cs`,
`tests/Camilo.OrderFlow.ArchitectureTests/CleanArchitectureTests.cs`. Inventario
`docs-develop/analisis/01-inventario.md` (C-2, D-5).
