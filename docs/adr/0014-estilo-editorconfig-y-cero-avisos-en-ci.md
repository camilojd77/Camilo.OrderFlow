# ADR-0014 — Estilo por `.editorconfig` en `suggestion` y cero avisos en CI

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** el equipo quiera que alguna regla de estilo rompa el build, o activar `TreatWarningsAsErrors`
  también en local.

## Contexto

El repo necesita un estilo común sin que preferencias de formato rompan el CI, y a la vez no acumular avisos del
compilador ni de NuGetAudit. Alternativas: reglas de estilo en `warning` con `EnforceCodeStyleInBuild` (más estricto,
más ruido), o no tratar avisos como errores en CI (los avisos se acumulan).

## Decisión

- `.editorconfig` describe el estilo que ya tiene el código, con todas las reglas en `suggestion`. Migraciones
  marcadas como código generado.
- `dotnet format --verify-no-changes` debe pasar (charset UTF-8 sin BOM, orden de `using`, espacios).
- El CI compila con `TreatWarningsAsErrors=true`: **cero avisos**. En local `TreatWarningsAsErrors=false` por
  ergonomía; antes de commitear se compila con `-p:TreatWarningsAsErrors=true` (skill `verificar-cambio`).
- No se añaden `NoWarn`, `#pragma warning disable` ni `[SuppressMessage]`, ni se bajan severidades para pasar: se
  corrige el código.

## Consecuencias

- Un aviso nuevo (incluida una vulnerabilidad nueva de NuGetAudit) rompe el CI aunque el código no cambie.
- En Git Bash hay que escribir `-p:` y no `/p:` (MSYS convierte `/p:` en ruta).

## Referencias

`.editorconfig`, `Directory.Build.props`, `.github/workflows/ci.yml`. Línea base en `CHANGELOG.md`.
