---
paths:
  - ".github/**"
  - "Directory.Build.props"
  - "Directory.Packages.props"
  - "global.json"
  - "nuget.config"
  - ".config/**"
  - "*.slnx"
  - "**/*.csproj"
  - ".editorconfig"
---

# Build, paquetes y CI

- Versiones **solo** en `Directory.Packages.props`; un `.csproj` nunca lleva `Version=`.
- No añadas ni actualices paquetes, herramientas ni imágenes salvo que te lo pidan, y entonces con la skill
  `actualizar-dependencias` (licencia, cadena con `dotnet nuget why`, una familia a la vez).
- Pines transitivos solo para cerrar vulnerabilidades, con el bloque de comentario «Llega por / Motivo / Revisar» del
  archivo.
- **No** añadas `NoWarn`, `WarningsAsErrors` relajados, `#pragma warning disable` ni `[SuppressMessage]`, ni bajes
  severidades de `.editorconfig` para pasar el build: corrige el código.
- `TreatWarningsAsErrors=true` en `Directory.Build.props` (local y CI): no lo desactives. El CI ejecuta todos los tests (RealDb incluida). No lo debilites (ni
  filtros, ni `continue-on-error`).
- Proyectos nuevos solo con la skill `nuevo-modulo`, y siempre en el `.slnx` en su carpeta virtual.
- `global.json` declara el runner de tests (Microsoft.Testing.Platform); no lo quites.

Referencia: `Directory.Packages.props` (sección «Transitive pins»), `.github/workflows/ci.yml`.

Fuente: [ADR-0012](../../docs/adr/0012-versiones-centralizadas-y-pines-transitivos.md) ·
[ADR-0023](../../docs/adr/0023-avisos-como-errores-en-local-y-ci.md) · [CLAUDE.md](../../CLAUDE.md) §8
