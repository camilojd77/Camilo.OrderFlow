# Índice de documentación

El punto de entrada es el [`README.md`](../README.md) de la raíz. Esta página dice qué leer según lo que necesites hacer.

## Por dónde empezar

| Si eres… | Lee, en este orden |
|---|---|
| **Desarrollador nuevo en el proyecto** | [`03-Guia-Nuevo-Equipo.md`](03-Guia-Nuevo-Equipo.md) → [`01-Arquitectura.md`](01-Arquitectura.md) → [`Standards.md`](Standards.md) |
| **Quien arranca un producto nuevo con la plantilla** | [`03-Guia-Nuevo-Equipo.md`](03-Guia-Nuevo-Equipo.md) → [`04-Guia-Renombrado-y-Repositorio.md`](04-Guia-Renombrado-y-Repositorio.md) |
| **Arquitecto o revisor de TI** | [`01-Arquitectura.md`](01-Arquitectura.md) → [`05-Plan-Definitivo.md`](05-Plan-Definitivo.md) → checklist de aprobación del [`README.md`](../README.md) |
| **Responsable de un deploy** | [`ReleaseChecklist.md`](ReleaseChecklist.md) → [`Observability.md`](Observability.md) |
| **Agente de IA (Copilot, Claude, Cursor)** | [`02-Instrucciones-IA.md`](02-Instrucciones-IA.md) → [`Standards.md`](Standards.md) |

## Todos los documentos

| Documento | Contenido |
|---|---|
| [`01-Arquitectura.md`](01-Arquitectura.md) | Visión estructural, reglas de dependencias, decisiones explícitas |
| [`02-Instrucciones-IA.md`](02-Instrucciones-IA.md) | Convenciones no negociables para agentes de IA que extiendan la plantilla |
| [`03-Guia-Nuevo-Equipo.md`](03-Guia-Nuevo-Equipo.md) | De `git clone` a la API corriendo: prerrequisitos, contenedores, migraciones, troubleshooting |
| [`04-Guia-Renombrado-y-Repositorio.md`](04-Guia-Renombrado-y-Repositorio.md) | Renombrado técnico, exclusiones y validación; otro repositorio es opción fuera de la fase local |
| [`05-Plan-Definitivo.md`](05-Plan-Definitivo.md) | Decisiones D1–D10 tomadas al construir la plantilla (**histórico**, ya ejecutado) |
| [`Standards.md`](Standards.md) | Patrones completos: módulo, entidad, command, query, endpoint, tests |
| [`Observability.md`](Observability.md) | Cómo usar Grafana, Loki, Tempo y Prometheus con esta API |
| [`EntenderObservabilidad.md`](EntenderObservabilidad.md) | Ruta de estudio de observabilidad moderna (material de formación, independiente de la plantilla) |
| [`ReleaseChecklist.md`](ReleaseChecklist.md) | Qué validar antes de cada deploy a Staging o Production |
| [`archive/06-Plan-HandOff.md`](archive/06-Plan-HandOff.md) | Plan de hand-off a TI (**ejecutado**) |
| [`archive/07-Plan-Cambios-Revision.md`](archive/07-Plan-Cambios-Revision.md) | Revisión completa de la plantilla y plan de cambios (**ejecutado**) |

> Los documentos de `archive/` documentan **cómo se llegó hasta aquí**; no describen el estado actual.

## Documentación fuera de `docs/`

| Ubicación | Contenido |
|---|---|
| [`../README.md`](../README.md) | Puerta de entrada: qué es la plantilla, arranque rápido, checklist de TI |
| [`../deploy/observability/README.md`](../deploy/observability/README.md) | Detalle del stack de observabilidad local: componentes, puertos, volúmenes |
| [`../Dockerfile`](../Dockerfile) | Imagen de la API, comentada etapa por etapa |
| [`../deploy/app/docker-compose.yml`](../deploy/app/docker-compose.yml) | Aplicación completa en contenedores (BBDD + migraciones + API) |
