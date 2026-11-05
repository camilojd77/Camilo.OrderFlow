# ADR-0022 — Los clientes HTTP de un módulo viven en su Infrastructure, detrás de un puerto

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** se implemente el primer cliente HTTP de un módulo (para fijar la referencia) o un cliente pase a
  ser compartido por varios módulos.

## Contexto

La guía antigua (`docs/archive/Standards.md`) decía «llamada a API externa → `*.Infrastructure/Http/`», pero ningún
módulo tiene cliente HTTP: el único, `ErpClient`, es un ejemplo de resiliencia registrado en el host
(`ServiceCollectionExtensions.Resilience.cs`). Hacía falta decidir dónde va un cliente que necesita un solo módulo.

Alternativas: todos los clientes en el host (el host acumularía conocimiento de cada integración y el módulo no se
podría extraer con ella) o clientes en Application (prohibido: Application no conoce HTTP).

## Decisión

- Un cliente HTTP que usa **un solo módulo** vive en `Camilo.OrderFlow.Modules.<M>.Infrastructure/Http/`, implementa un
  **puerto** de `<M>.Application/Abstractions` (con tipos propios del módulo, no DTOs del servicio externo) y se
  registra en `Add<M>Infrastructure` con `AddHttpClient`.
- Su configuración va en `appsettings` bajo una sección propia, con opciones validadas al arrancar; si necesita
  resiliencia, sigue la forma de `ErpClient` (sección `Resilience:<Cliente>`, timeout + reintentos + circuit breaker)
  y sus reintentos solo cubren operaciones idempotentes.
- El **host** solo registra clientes transversales (compartidos o de infraestructura) y los adaptadores entre módulos
  ([ADR-0006](0006-integracion-modulos-adaptador-en-host.md)).

## Consecuencias

- La integración viaja con el módulo si se extrae.
- El primer cliente de un módulo con resiliencia necesitará `Microsoft.Extensions.Http.Resilience` en el
  `.csproj` de ese Infrastructure (hoy solo lo referencia el host): se añade con la skill `actualizar-dependencias`.
- Propagación de traza automática por la instrumentación de `HttpClient`; `CorrelationIdHandler` está en el host y, si
  el módulo lo necesita, se decide cómo exponerlo en ese primer cliente.

## Referencias

`Camilo.OrderFlow.WebApi/Extensions/ServiceCollectionExtensions.Resilience.cs`, `ResilienceOptions.cs`,
`.claude/rules/infraestructura-datos.md`, `docs/architecture.md` §5.
