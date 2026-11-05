---
name: commit
description: "Crea un commit en Camilo.OrderFlow con Conventional Commits en inglés, sin atribución de IA, mensaje en archivo sin BOM y git commit -F. Nunca hace push ni usa --no-verify."
argument-hint: "[tipo(scope): asunto]"
disable-model-invocation: true
---

# Commit

1. Si no se ha verificado el cambio en esta sesión, ejecuta antes la skill `verificar-cambio`.
2. `git status --short` y `git diff --cached --stat`. Añade solo los archivos del cambio, por ruta
   (`git add <ruta>…`); nunca `git add -f` ni archivos ignorados.
3. Mensaje en **inglés**, [Conventional Commits](https://www.conventionalcommits.org/):
   - Asunto `type(scope): subject` en imperativo, ≤ 72 caracteres, sin punto final. Tipos: `feat`, `fix`, `refactor`,
     `perf`, `test`, `docs`, `style`, `chore`, `build`, `ci`. Scope = módulo o área (`identity`, `webapi`, `adr`,
     `claude`, `deps`…).
   - Cuerpo: el **porqué** y lo que no es obvio en el diff, líneas ≤ 72.
   - **Sin atribución de IA**: ni `Co-Authored-By`, ni `Generated with`, ni mención a un modelo o asistente.
4. Escribe el mensaje en un archivo **UTF-8 sin BOM** con la herramienta de escritura de archivos (en el scratchpad,
   nunca dentro del repo) y ejecuta `git commit -F <archivo>`. Nunca `-m` con here-string de PowerShell 5.1.
5. Nunca `--no-verify`, `-n`, `--amend` sobre commits ya publicados, ni `git push`: el push lo hace la persona.
6. Muestra `git log --oneline -1` y el hash.
7. **Si con este commit la rama corta queda terminada y estable** (verificación en verde, nada pendiente): intégrala
   en `develop` con `git switch develop` y `git merge --no-ff <rama>`. Comprueba `git status` limpio y borra la rama
   con `git branch -d <rama>`. No toques `main` salvo que la persona lo pida
   ([ADR-0021](../../../docs/adr/0021-main-principal-y-develop-de-desarrollo.md)).

La identidad de autor es la de `git config` (local del repo). Si no hay, pregunta; no la inventes.
