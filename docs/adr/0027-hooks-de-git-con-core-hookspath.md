# ADR-0027 — Hooks de git versionados con `core.hooksPath`

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** el `pre-push` tarde tanto que la gente lo evite, o el equipo crezca y haga falta instalación
  automática de los hooks.

## Contexto

La verificación antes de commitear dependía de acordarse de la skill `verificar-cambio`, y las reglas de los
mensajes (Conventional Commits en inglés, sin atribución de IA) solo estaban escritas. Un commit con un formato
incorrecto o con `Co-Authored-By` llegaba al historial sin que nada lo parase.

Alternativas: Husky.Net (herramienta dotnet nueva en el manifiesto, se instala sola con `dotnet tool restore`, pero es
una dependencia que mantener) o no tener hooks (todo depende del CI, que avisa tarde).

## Decisión

- Hooks en `.githooks/` como scripts `sh` (los ejecuta Git for Windows y cualquier Linux), sin dependencias nuevas.
- Cada persona los activa una vez con `git config core.hooksPath .githooks`.
- **`commit-msg`**: asunto en Conventional Commits (`tipo(scope): asunto`, ≤ 72 caracteres) y ninguna atribución ni
  mención a un modelo o asistente. Acepta los mensajes que genera git (`Merge …`, `Revert "…"`, `fixup!`).
- **`pre-commit`**: `dotnet format --verify-no-changes` sobre los `.cs` en el commit (solo si los hay).
- **`pre-push`**: build Release (avisos = errores) y suite rápida sin RealDb. La suite completa y el umbral de
  cobertura quedan para el CI ([ADR-0026](0026-umbral-de-cobertura-por-proyecto.md)).
- Nunca se saltan con `--no-verify` (también denegado en `.claude/settings.json`).

## Consecuencias

- Los errores de formato y de mensaje se ven antes de que salgan de la máquina.
- `pre-commit` añade unos segundos por commit con `.cs` y `pre-push` alrededor de medio minuto.
- `pre-commit` lee el archivo del disco: si solo una parte está en el índice, comprueba el archivo entero.
- Un clon nuevo no tiene hooks hasta ejecutar el `git config`; está en el README y en `CLAUDE.md` §1.

## Referencias

`.githooks/commit-msg`, `.githooks/pre-commit`, `.githooks/pre-push`, `eng/probar-commit-msg.sh`, `.gitattributes`,
`README.md`, `CLAUDE.md` §1 y §6, skill `commit`.
