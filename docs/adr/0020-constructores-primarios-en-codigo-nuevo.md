# ADR-0020 — Constructores primarios en código nuevo

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** se decida migrar el código existente o subir la severidad de la regla.

## Contexto

Todo el código existente usa constructores clásicos que asignan campos `_camelCase`, y `.editorconfig` lo reforzaba
con `csharp_style_prefer_primary_constructors = false`. C# 12+ permite constructores primarios, que eliminan el
código repetitivo de la inyección de dependencias.

## Decisión

- **Código nuevo** (clases nuevas: handlers, servicios, repositorios, validadores con dependencias) usa
  **constructores primarios** para la inyección de dependencias. Si un parámetro se guarda, se asigna a un campo
  `private readonly` con nombre `_camelCase`; si no, se usa el parámetro directamente.
- Las entidades de Domain mantienen su patrón (constructor privado para EF + constructor público que valida).
- **No** se migra el código existente.
- `.editorconfig`: `csharp_style_prefer_primary_constructors = true:suggestion` (sugerencia, no rompe el build).

## Consecuencias

- Conviven dos estilos hasta que se migre lo existente.
- El IDE sugerirá constructores primarios también en archivos existentes: es una sugerencia, no hay que aplicarla al
  pasar por ellos.

## Referencias

`.editorconfig` (sección C#), skills `nueva-funcionalidad` y `nuevo-modulo`.
