# ADR-0021 — Una sola rama de larga vida: `develop`

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** haya un entorno de producción que necesite una rama de versiones publicadas, o más de una
  persona integrando cambios a la vez.

## Contexto

El repo nació con `main` (rama por defecto) y `develop` como rama de trabajo, y la documentación decía que `main`
solo recibía merges desde `develop`. En la práctica `main` se había quedado en el commit previo al renombrado y nadie
integraba en ella. El 2026-11-05 se borró `main` del remoto y `develop` pasó a ser la rama por defecto.

Alternativas: mantener `main` + `develop` (Git Flow simplificado: dos ramas que sincronizar sin un despliegue que lo
justifique) o *trunk-based* sobre `main` (habría que renombrar la rama y reconfigurar el CI).

## Decisión

- **`develop` es la única rama de larga vida** y la rama por defecto del repositorio en GitHub.
- Todo cambio entra por una rama corta `<tipo>/<tema-corto>` (tipos de Conventional Commits) y un PR hacia `develop`.
- No existe `main`. Si algún día hace falta marcar versiones publicadas, se usan etiquetas (`vX.Y.Z`) o un ADR nuevo.

## Consecuencias

- Un solo destino de integración; menos sincronización.
- `.github/workflows/ci.yml` aún declara `main` en sus disparadores: no hace daño, pero sobra; quitarlo es una
  propuesta (cambio de pipeline).
- Los clones existentes conservan una rama `main` local antigua: se puede borrar (`git branch -D main`).

## Referencias

`README.md` § Contribuir, `CLAUDE.md` §1 y §6, `.github/workflows/ci.yml`. Inventario
`docs-develop/analisis/01-inventario.md` (D-2, reemplazada por esta decisión).
