# ADR-0023 — Avisos como errores en local y en CI; estilo por `.editorconfig` en `suggestion`

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** el equipo quiera que alguna regla de estilo rompa el build, o un aviso del SDK o de un analizador
  nuevo bloquee el trabajo sin arreglo posible.

## Contexto

Reemplaza a [ADR-0014](0014-estilo-editorconfig-y-cero-avisos-en-ci.md). Este dejaba `TreatWarningsAsErrors=false` en
local por ergonomía y solo lo activaba en el CI. La consecuencia era que un aviso aparecía por primera vez al llegar al
CI (pasó con las vulnerabilidades NU1902/NU1903) y que la verificación local dependía de acordarse de pasar
`-p:TreatWarningsAsErrors=true`. El repo está en cero avisos, así que activarlo en local no rompe nada hoy.

Alternativas: mantener ADR-0014 (el problema descrito), o activar además `EnforceCodeStyleInBuild` con reglas en
`warning` (el estilo rompería el build; demasiado estricto para preferencias de formato).

## Decisión

- `TreatWarningsAsErrors=true` en `Directory.Build.props`: **cualquier aviso es un error**, en local y en CI.
- `.editorconfig` describe el estilo con todas las reglas en `suggestion` (no rompe el build); `dotnet format
  --verify-no-changes` debe pasar. Migraciones marcadas como código generado.
- No se añaden `NoWarn`, `#pragma warning disable` ni `[SuppressMessage]`, ni se bajan severidades: se corrige el
  código. Una excepción necesita un ADR propio.

## Consecuencias

- Un aviso (incluida una vulnerabilidad nueva detectada por NuGetAudit) rompe la compilación local en cuanto aparece,
  aunque el código no haya cambiado: se trata como cualquier error (skill `actualizar-dependencias` si es un paquete).
- El CI sigue pasando `/p:TreatWarningsAsErrors=true`; ahora es redundante y no hace daño.
- Los comandos ya no necesitan `-p:TreatWarningsAsErrors=true`.

## Referencias

`Directory.Build.props`, `.editorconfig`, `.github/workflows/ci.yml`, skill `verificar-cambio`.
