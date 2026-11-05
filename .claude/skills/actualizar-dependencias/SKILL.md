---
name: actualizar-dependencias
description: "Añade o actualiza paquetes NuGet, herramientas dotnet o imágenes en Camilo.OrderFlow, solo cuando se pide explícitamente: CPM, una familia a la vez, vulnerabilidades, licencias y verificación completa."
argument-hint: "<paquete o familia> [versión]"
disable-model-invocation: true
---

# Añadir o actualizar dependencias

Solo se ejecuta si la persona lo pidió. Nunca como efecto secundario de otra tarea.

1. **Estado actual.** `dotnet list Camilo.OrderFlow.slnx package --outdated`,
   `dotnet list Camilo.OrderFlow.slnx package --vulnerable --include-transitive`, y para un paquete concreto
   `dotnet nuget why <proyecto> <paquete>`.
2. **Una familia a la vez** (p. ej. todo `Microsoft.EntityFrameworkCore.*` junto, todo `OpenTelemetry.*` junto).
3. **Licencia.** Comprueba la licencia de la versión destino en nuget.org. MediatR, AutoMapper, FluentAssertions y
   MassTransit pasaron a licencia comercial: no se usan ([ADR-0004](../../../docs/adr/0004-dispatcher-propio-y-mapperly.md)).
   Si una licencia cambió, para y pregunta.
4. **Cambios rompedores.** Lee las notas de versión de la versión destino y anota lo que afecte al repo.
5. **Editar solo `Directory.Packages.props`** (versiones) y, si es un paquete nuevo, el `<PackageReference>` sin
   versión en el `.csproj` que lo usa. Respeta los grupos (`Label`) del archivo.
6. **Pin transitivo** solo para cerrar una vulnerabilidad cuyo padre no publica arreglo: entrada en
   `ItemGroup Label="Transitive pins"` y bloque de comentario «Llega por / Motivo (GHSA y severidad) / Revisar». Saca
   la versión corregida de la asesoría, no de memoria. Revisa si algún pin existente ya sobra y quítalo.
7. **Herramientas** (`.config/dotnet-tools.json`): `dotnet-ef` igual o más nueva que EF Core.
   **SDK, imágenes y acciones** ([ADR-0024](../../../docs/adr/0024-versiones-fijadas-sdk-imagenes-y-acciones.md)), solo
   si se pide y en un único cambio:
   - SDK: `global.json` y `sdk:`/`aspnet:` del `Dockerfile` (el runtime es el que trae ese SDK; compruébalo con
     `dotnet --info`). Verifica que las etiquetas existen en `mcr.microsoft.com/v2/dotnet/<imagen>/tags/list`.
   - SQL Server: la misma etiqueta exacta en `deploy/app`, `deploy/database` y `SqlServerFixture.cs`.
   - Acciones: `git ls-remote https://github.com/<owner>/<repo> refs/tags/<tag>` y `uses: <owner>/<repo>@<sha> # <tag>`.
8. **Verifica** con la skill `verificar-cambio` completa (RealDb incluida) y
   `dotnet list … package --vulnerable --include-transitive` vacío.
9. Commit `build(deps): …` o `fix(deps): …` con la skill `commit`; en el cuerpo, versiones antes → después y por qué.
