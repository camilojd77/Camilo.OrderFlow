---
name: verificar-cambio
description: "Verifica un cambio antes de commitear en Camilo.OrderFlow: formato, build con avisos como errores, tests del área tocada (RealDb si toca persistencia), arquitectura, vulnerabilidades y staging limpio. Úsala cuando termines un cambio de código o configuración, o antes de /commit. Resume con una tabla y cifras."
argument-hint: "[proyecto o área tocada]"
---

# Verificar un cambio

Ejecuta cada comando **de verdad** y resume con cifras. No des nada por bueno sin su salida. En Git Bash usa `-p:`,
no `/p:`.

1. **Qué cambió.** `git status --short` y `git diff --stat`. Identifica los proyectos tocados.
2. **Formato.** `dotnet format Camilo.OrderFlow.slnx --verify-no-changes`. Si falla, `dotnet format` sobre los
   archivos tocados y revisa que no toque nada más (ni `Migrations/`).
3. **Build como el CI.** `dotnet build Camilo.OrderFlow.slnx -c Release -p:TreatWarningsAsErrors=true`.
   Debe dar **0 avisos y 0 errores**. Un aviso no se silencia (`NoWarn`, `#pragma`): se corrige.
4. **Tests.**
   - Siempre: `dotnet test --no-build -c Release -- --filter-not-trait "Category=RealDb" --ignore-exit-code 8`
     (unitarios, arquitectura e integración SQLite).
   - Si el cambio toca persistencia, migraciones, consultas o el host y `docker info` responde:
     `dotnet test --no-build -c Release -- --filter-trait "Category=RealDb" --ignore-exit-code 8`. Comprueba que se
     ejecutaron casos (> 0). Si Docker no responde, dilo; no lo arranques tú.
   - Cobertura (informativa, sin umbral): añade `--coverage --coverage-output-format cobertura` y reporta el % de líneas
     del proyecto tocado.
5. **Dependencias** (si cambió `Directory.Packages.props` o un `.csproj`):
   `dotnet list Camilo.OrderFlow.slnx package --vulnerable --include-transitive` → ninguno.
6. **Staging limpio.** Nada de `bin/`, `obj/`, `TestResults/`, `logs/`, `*.user`, `.vs/`,
   `appsettings.Development.json`, `.env*`, `CLAUDE.local.md`, `.claude/settings.local.json`, ni secretos en el diff
   (`git diff --cached` revisado a ojo en archivos de configuración).
7. **Documentación.** Si el cambio deja desactualizado `CLAUDE.md`, `docs/`, `README.md`, un ADR o `.claude/`, dilo:
   se corrige en el mismo PR.

## Resumen esperado

| Comprobación | Resultado |
|---|---|
| Formato | OK / N archivos |
| Build (TWAE) | N avisos, N errores |
| Tests rápidos | aprobados/total por proyecto |
| RealDb | aprobados/total, o «no ejecutado: motivo» |
| Cobertura del proyecto tocado | NN,N % líneas |
| Vulnerables | ninguno / lista |
| Staging | limpio / qué sobra |
| Documentación | al día / qué falta |
