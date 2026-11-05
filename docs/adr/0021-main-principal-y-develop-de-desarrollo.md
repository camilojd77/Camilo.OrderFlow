# ADR-0021 — `main` como rama principal y `develop` como rama de desarrollo

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** haya un pipeline de despliegue que necesite ramas o etiquetas de versión, o más de una persona
  integrando cambios a la vez.

## Contexto

El repo nació con `main` (rama por defecto) y `develop`. `main` se quedó en el commit previo al renombrado y se borró
del remoto el 2026-11-05; durante unas horas `develop` fue la única rama. Se decidió recuperar `main` como rama
principal del repositorio y mantener `develop` como la rama donde se integra el desarrollo.

Alternativas: una sola rama `develop` (sin una rama que represente el estado publicado) o *trunk-based* sobre `main`
(todo el trabajo iría directo a la rama principal).

## Decisión

- **`main`** es la rama principal y la rama por defecto en GitHub. Solo avanza con merges desde `develop`, cuando el
  responsable del repositorio lo decide.
- **`develop`** es la rama de desarrollo: todo trabajo sale de ella y vuelve a ella.
- Cada cambio se hace en una rama corta `<tipo>/<tema-corto>` (tipos de Conventional Commits) creada desde `develop`.
  Cuando el cambio está **terminado y estable** (skill `verificar-cambio` en verde), se integra con un merge local a
  `develop` (`git merge --no-ff`) y la rama corta se borra.
- Los push los hace una persona: `develop` cada cierto tiempo y `main` cuando se decide publicar.

## Consecuencias

- `main` refleja lo publicado; `develop` puede ir por delante.
- El CI (`.github/workflows/ci.yml`) ya se dispara en push y PR a `main` y `develop`.
- Los cambios llegan a `develop` sin PR: la revisión es la verificación local y, si se quiere una segunda mirada, el
  subagente `revisor-arquitectura` antes del merge.

## Referencias

`README.md` § Contribuir, `CLAUDE.md` §1 y §6, `.claude/skills/commit/SKILL.md`, `.github/workflows/ci.yml`.
