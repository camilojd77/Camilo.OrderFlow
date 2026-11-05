# ADR-0012 — Versiones centralizadas (CPM) y pines transitivos solo por seguridad

- **Estado:** Reemplazada por [ADR-0024](0024-versiones-fijadas-sdk-imagenes-y-acciones.md)
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** se decida fijar el SDK en `global.json` o usar archivos de bloqueo (`packages.lock.json`).

## Contexto

Con varios proyectos, poner versiones en cada `.csproj` produce versiones distintas del mismo paquete. Además NuGetAudit
avisa de vulnerabilidades en paquetes transitivos que el repo no referencia directamente y cuyo padre aún no publica
arreglo (ya pasó con `SQLitePCLRaw.lib.e_sqlite3` y `System.Security.Cryptography.Pkcs`).

## Decisión

- **Central Package Management**: todas las versiones en `Directory.Packages.props`; los `.csproj` nunca llevan
  `Version=`.
- `CentralPackageTransitivePinningEnabled=true`, usado **solo** para cerrar avisos de seguridad. Cada pin lleva en el
  comentario del archivo: por qué paquete llega, la asesoría (GHSA) y cuándo quitarlo.
- Solo nuget.org como origen (`nuget.config` con `<clear/>`).
- El SDK **no** está fijado: `global.json` solo elige el runner de tests; el CI usa `10.0.x`.
- Añadir o actualizar paquetes solo cuando se pide, con la skill `actualizar-dependencias`.

## Consecuencias

- Una versión por paquete en toda la solución y un único sitio que revisar.
- El SDK puede variar entre máquinas y CI (hoy 10.0.403 en local); fijarlo es propuesta abierta.
- Los pines transitivos hay que revisarlos en cada actualización del paquete padre y quitarlos cuando sobren.

## Referencias

`Directory.Packages.props` (sección «Transitive pins»), `nuget.config`, `global.json`, `.github/workflows/ci.yml`.
