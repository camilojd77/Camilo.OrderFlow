> **Archivado el 2026-11-05.** El producto ya está renombrado a Camilo.OrderFlow. Solo es útil si se reutiliza `dev/rename.ps1` sobre una copia de la plantilla original. Se conserva como histórico: no describe el estado actual y no se actualiza; sus enlaces internos pueden no resolver.

# Documento 4 — Renombrado técnico y conservación del repositorio

Producto: **Camilo.OrderFlow**, variantes `camilo-order-flow` y `CamiloOrderFlow`; base local nueva `CamiloOrderFlowDev`. La fase local usa exactamente `develop`, conserva raíz física, `.git`, historia, remotos y cambios previos. No crea commits ni hace push.

## Preparación

Leer la [guía de arranque](03-Guia-Nuevo-Equipo.md), revisar status, diff staged/no staged, ramas locales y referencias remotas locales. Si develop existe, comparar su base antes de cambiar; ante divergencia o conflicto detenerse sin resetearla ni recrearla. Inventariar proyectos desde .slnx, archivos afectados y destinos. Respaldos acotados sin secretos; planes, análisis, prompts e informes personales en docs-develop ignorado.

`dev/rename.ps1` conserva intencionalmente el origen `Company.Product`, `company-product` y `CompanyProduct`. Es una herramienta para derivar la plantilla; no renombra arbitrariamente este producto ya derivado.

## Exclusiones y simulación

El script recorre el filesystem y puede tocar archivos ignorados: .gitignore no lo limita. Excluye Git, IDE, build, docs-develop, .agents, .codex, .aws y directorios de datos bajo deploy. Configuraciones reales appsettings.Development.json, appsettings.Local.json, appsettings.Production.json, appsettings.Staging.json, secrets.json y .env se manejan selectivamente sin imprimir credenciales. Las plantillas .example pueden cambiar; verificarlas explícitamente porque su extensión no está en la lista de texto del script. docs-develop/ también debe estar en .dockerignore: Dockerfile usa COPY . .

Si hace falta limpiar build, enumerar únicamente bin/obj de proyectos declarados en .slnx; resolver y verificar cada ruta absoluta dentro de la raíz antes de Remove-Item -LiteralPath. No borrar .vs, datos ni configuración del usuario; no matar procesos ajenos.

En un checkout que todavía tenga nombres de plantilla:

```powershell
$ProductName = 'Camilo.OrderFlow'
./dev/rename.ps1 -NewName $ProductName -WhatIf
# Revisar alcance, hashes y colisiones antes de aplicar una sola vez.
./dev/rename.ps1 -NewName $ProductName
```

Revisar archivos ocultos con git ls-files, .github y .config. Si falla a mitad, detener sustituciones, comparar inventario/destinos/respaldos y completar solo pendientes mecánicos inequívocos. No repetir ciegamente ni invertir contenido globalmente.

## Verificación técnica y configuración

Comprobar solución, proyectos, namespaces, ProjectReference, Dockerfile, scripts, ModuleCatalog, Swagger, JWT, ServiceName, labels de Loki/Grafana, Compose y .http. Contar archivos en cada checkout: no hay un número universal.

Conservar Program, IDs NuGet externos, contratos HTTP, permisos, schema identity, timestamps/IDs y operaciones de migraciones e historial identity.__EFMigrationsHistory. No regenerar migraciones para renombrar; solo cambiar namespaces/referencias técnicas.

Conservar admin@company.com como demostración y puertos actuales: API 5070/7297, SQL local 1433, app en contenedor 8080/1434 y telemetría según Compose. No inventar dominios. Ajustar conexiones locales a CamiloOrderFlowDev sin perder credenciales; consultar si ya tiene datos. No modificar producción/staging, borrar ni migrar automáticamente bases anteriores.

Compose puede cambiar prefijos de volúmenes por el proyecto nuevo; los anteriores quedan conservados. No usar down -v ni wipe. El compose database conserva proyecto implícito de su carpeta; observabilidad mantiene red y nombres de contenedores compartidos, posibles colisiones con otro stack.

## Validaciones y aceptación

Desde la raíz, con Microsoft Testing Platform:

```powershell
$ProductName = 'Camilo.OrderFlow'
dotnet restore "$ProductName.slnx"
dotnet build "$ProductName.slnx" --no-restore /p:TreatWarningsAsErrors=true
dotnet test --no-build --no-restore -- --filter-not-trait "Category=RealDb" --ignore-exit-code 8
dotnet test --no-build --no-restore -- --filter-trait "Category=RealDb" --ignore-exit-code 8
git diff --check
git branch --show-current
```

Código 8 significa ausencia de casos en un proyecto filtrado; ignorarlo no valida SQL. RealDb exige Docker operativo y casos SQL realmente ejecutados con Testcontainers; LocalDB no sustituye esa suite. Registrar cantidades y fallos.

Aplicar migraciones con la herramienta del manifiesto, arrancar Development y verificar Swagger, login, /api/users/me/permissions, /health/live, /health/ready y /_info. Validar Compose sin imprimir secretos (config --quiet), imagen, migrator y telemetría recibida en Grafana. Tener configuración no acredita observabilidad.

- [ ] develop antes de editar; historia, remotos y cambios previos preservados.
- [ ] Solución coherente; build sin warnings/errores.
- [ ] Tests rápidos y RealDb aprobados con cantidades.
- [ ] Migraciones sin cambios semánticos; base nueva y datos previos conservados.
- [ ] Arranque, endpoints, contenedores y observabilidad comprobados o pendientes explícitos.
- [ ] Cero referencias antiguas ACTIVAS incorrectas con excepciones inventariadas.
- [ ] Documentación en español; código nuevo/modificado e identificadores en inglés.
- [ ] docs-develop ignorado y excluido de script/Docker; sin stage forzado, commit ni push.

Buscar también archivos nuevos no staged y ocultos con rg, excluyendo build, Git, datos y docs personales. Excepciones: origen de rename.ps1, esta explicación del origen, raíz física conservada, Git y documentos históricos/personales. No usar cero coincidencias globales como única prueba.

## Otro repositorio: opciones fuera de esta fase

Cambiar remotos/historia, publicar, proteger ramas o mover la raíz física requieren tarea y autorización propias; no son pasos universales del renombrado local.

CI actual: push/PR a main/develop; SDK 10.0.x; restore, build con warnings como errores, tests sin filtro (incluye RealDb), TRX y cobertura. Sin nightly programado, umbral de cobertura ni auditoría dedicada de vulnerabilidades. global.json selecciona runner sin fijar SDK; paquetes centrales no equivalen a resolución completamente bloqueada.
