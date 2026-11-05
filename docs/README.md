# Índice de documentación

El punto de entrada es el [`README.md`](../README.md) de la raíz; el manual operativo, [`CLAUDE.md`](../CLAUDE.md).
Cada tema tiene **una sola fuente**; los demás documentos enlazan a ella.

## Por dónde empezar

| Si eres… | Lee, en este orden |
|---|---|
| Desarrollador nuevo | [`README.md`](../README.md) → [`CLAUDE.md`](../CLAUDE.md) → [`architecture.md`](architecture.md) |
| Arquitecto o revisor | [`architecture.md`](architecture.md) → [`adr/`](adr/README.md) → [`CHANGELOG.md`](../CHANGELOG.md) |
| Responsable de un despliegue | [`ReleaseChecklist.md`](ReleaseChecklist.md) → [`observabilidad.md`](observabilidad.md) |
| Modelo de IA (Claude u otro) | [`CLAUDE.md`](../CLAUDE.md); las reglas de `.claude/rules/` se cargan solas por ruta y las recetas están en `.claude/skills/` |

## Documentos vigentes

| Documento | Fuente única de |
|---|---|
| [`../README.md`](../README.md) | Estado del proyecto, requisitos, desarrollo local, comandos, resolución de problemas |
| [`../CLAUDE.md`](../CLAUDE.md) | Manual operativo para personas y modelos de IA |
| [`../CHANGELOG.md`](../CHANGELOG.md) | Historial de cambios con cifras |
| [`architecture.md`](architecture.md) | Arquitectura: proyectos, reglas de dependencia, cadena de un caso de uso, datos, API, ruta a microservicios |
| [`observabilidad.md`](observabilidad.md) | Flujo de señales, catálogo, convenciones de telemetría, stack local, runbooks |
| [`adr/`](adr/README.md) | Decisiones de arquitectura y su porqué |
| [`ReleaseChecklist.md`](ReleaseChecklist.md) | Qué validar antes de cada despliegue |
| [`EntenderObservabilidad.md`](EntenderObservabilidad.md) | Ruta de estudio de observabilidad (formación general; no fija convenciones del repo) |
| [`../deploy/observability/README.md`](../deploy/observability/README.md) | Detalle del stack local: componentes, puertos, volúmenes |

## Archivo (histórico)

`archive/` documenta **cómo se llegó hasta aquí**; no describe el estado actual y no se actualiza. Cada documento
dice al principio qué lo reemplaza.

| Documento | Contenido |
|---|---|
| [`archive/01-Arquitectura.md`](archive/01-Arquitectura.md) | Guía de arquitectura de la plantilla |
| [`archive/02-Instrucciones-IA.md`](archive/02-Instrucciones-IA.md) | Instrucciones anteriores para agentes de IA |
| [`archive/03-Guia-Nuevo-Equipo.md`](archive/03-Guia-Nuevo-Equipo.md) | Puesta en marcha paso a paso |
| [`archive/04-Guia-Renombrado-y-Repositorio.md`](archive/04-Guia-Renombrado-y-Repositorio.md) | Renombrar la plantilla con `dev/rename.ps1` |
| [`archive/05-Plan-Definitivo.md`](archive/05-Plan-Definitivo.md) | Plan de construcción de la plantilla (decisiones D1-D10) |
| [`archive/06-Plan-HandOff.md`](archive/06-Plan-HandOff.md) | Plan de hand-off a TI |
| [`archive/07-Plan-Cambios-Revision.md`](archive/07-Plan-Cambios-Revision.md) | Revisión de la plantilla y plan de cambios |
| [`archive/Standards.md`](archive/Standards.md) | Recetas de módulo, command, query, endpoint y tests |
| [`archive/Observability.md`](archive/Observability.md) | Guía rápida anterior de observabilidad |
