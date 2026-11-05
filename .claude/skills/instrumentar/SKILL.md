---
name: instrumentar
description: "Añade un span, una métrica o un log a Camilo.OrderFlow siguiendo las convenciones de docs/observabilidad.md, actualiza el catálogo de señales y lo verifica en el stack local (Tempo, Prometheus, Loki). Úsala cuando un flujo nuevo necesite observarse."
argument-hint: "<flujo o componente>"
---

# Instrumentar

Lee antes [`docs/observabilidad.md`](../../../docs/observabilidad.md) §3-4: catálogo y convenciones. Hoy no hay
`ActivitySource`, `Meter` ni logs propios: lo primero que escribas establece la referencia.

1. **¿Qué señal?** Span = una operación con duración y causalidad (llamada externa, paso costoso). Métrica = algo que
   se cuenta o mide para alertar (pedidos creados, duración de un proceso). Log = un hecho puntual con contexto. Usa la
   mínima que responda la pregunta.
2. **Log** ([ADR-0018](../../../docs/adr/0018-loggermessage-para-logs-nuevos.md)): `ILogger<T>` inyectado y métodos
   `static partial` con `[LoggerMessage(EventId = …, Level = …, Message = "… {PropiedadPascal} …")]` en la clase o en
   una clase `Log` anidada. Sin interpolación ni datos personales.
3. **Span**: `ActivitySource` del módulo `Camilo.OrderFlow.<Modulo>` (una instancia estática por módulo).
   `StartActivity("<operación>")`, atributos con convenciones semánticas, `SetStatus(ActivityStatusCode.Error)` en
   fallos. Regístralo con `AddSource(...)` en
   `Camilo.OrderFlow.WebApi/Observability/OpenTelemetry/OpenTelemetryExtensions.cs`.
4. **Métrica**: `Meter` del módulo creado por `IMeterFactory` (`Camilo.OrderFlow.<Modulo>`), instrumento con nombre en
   minúsculas con puntos y **unidad**; etiquetas de baja cardinalidad (nunca IDs ni correos). Regístralo con
   `AddMeter(...)` en el mismo archivo.
5. **Catálogo**: añade la fila en `docs/observabilidad.md` §3.2 (tipo, nombre, unidad/etiquetas, dónde se crea).
6. **Test**: si la señal tiene lógica (etiquetas calculadas), cúbrela con un test unitario.
7. **Verifica en local**: `.\dev\observability.ps1 up`, API con `--launch-profile Local+OTel`, genera tráfico y
   comprueba con la skill `diagnosticar` que la traza aparece en Tempo, la métrica en Prometheus (job
   `otel-collector-metrics`) o el log en Loki.
8. Skill `verificar-cambio`.
