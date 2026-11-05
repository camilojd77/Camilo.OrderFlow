# ADR-0001 — Registrar las decisiones de arquitectura con ADR

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** el equipo adopte otra herramienta de registro de decisiones (wiki, RFC) para todos sus repos.

## Contexto

Las decisiones de la plantilla estaban repartidas en `01-Arquitectura.md` §9, `02-Instrucciones-IA.md`, el plan
histórico `05-Plan-Definitivo.md` y comentarios de código. Varias se contradecían entre sí (p. ej. dónde vive el
adaptador entre módulos) y nada decía cuál estaba vigente. Las personas nuevas y los modelos de IA no tenían forma de
saber qué es «así a propósito» y qué es deuda.

Alternativas: seguir con secciones de decisiones dentro de cada guía (se desactualizan y se duplican) o una wiki fuera
del repo (no viaja con el código ni con el PR que cambia la decisión).

## Decisión

Cada decisión vigente con alternativas reales se registra como un ADR en `docs/adr/NNNN-<titulo>.md` con la
[plantilla](plantilla.md). Un ADR aceptado **no se edita**: si la decisión cambia, se escribe uno nuevo que lo
reemplaza y el antiguo pasa a «Reemplazada por». El índice [`README.md`](README.md) y la línea correspondiente en
`CLAUDE.md` §7 se actualizan en el mismo PR.

## Consecuencias

- Una sola fuente para las decisiones; `CLAUDE.md`, `docs/architecture.md` y las reglas de `.claude/rules/` enlazan al
  ADR en vez de repetir el porqué.
- Cambiar una decisión cuesta un ADR nuevo; a cambio queda el historial.
- La skill `nuevo-adr` automatiza número, plantilla, índice y línea en `CLAUDE.md`.

## Referencias

`docs/adr/README.md`, `docs/adr/plantilla.md`, `.claude/skills/nuevo-adr/SKILL.md`. Plan
`docs-develop/planes/01-plan-estructura-claude.md`.
