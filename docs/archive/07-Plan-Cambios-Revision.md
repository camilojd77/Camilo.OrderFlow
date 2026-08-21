# Documento 7 — Plan de cambios propuesto tras revisión de la plantilla

> **Autor:** revisión asistida (Claude Opus 5) — 2026-08-20
> **Estado:** ✅ **EJECUTADO (2026-08-20).** Documento histórico: se conserva como registro de qué se revisó, qué se decidió y qué se cambió. El estado actual de la plantilla está en [`../../README.md`](../../README.md) y [`../03-Guia-Nuevo-Equipo.md`](../03-Guia-Nuevo-Equipo.md).
>
> **Resultado de la ejecución**
>
> | Fase | Estado | Evidencia |
> |---|---|---|
> | 1 — Vulnerabilidades | ✅ | `dotnet list package --vulnerable --include-transitive` vacío; `build /p:TreatWarningsAsErrors=true` con 0 warnings |
> | 2 — Código (C1–C5) | ✅ | Suite rápida 200/200; architecture tests 17 → 20 |
> | 3 — Contenedores y tooling | ✅ | `docker build` correcto; `docker compose up` levanta BBDD + migraciones + API; login del admin y `/health/ready` verdes |
> | 4 — Documentos nuevos | ✅ | `03-Guia-Nuevo-Equipo.md`, `04-Guia-Renombrado-y-Repositorio.md`, `docs/README.md`, `dev/rename.ps1` (probado con `-WhatIf`) |
> | 5 — Correcciones de docs | ✅ | E1–E13 aplicadas; 0 enlaces internos rotos |
> | 6 — Cierre | ✅ | Release: 200 tests rápidos + 6 RealDb en verde; sin paquetes vulnerables |
>
> **Desviaciones respecto a lo planificado** (todas menores, detectadas al verificar):
> - `Testcontainers.MsSql` 4.14 obsoleta el constructor sin parámetros de `MsSqlBuilder`: se ajustó `SqlServerFixture.cs` (una línea).
> - `dotnet-ef` se fijó en **10.0.11**, no 10.0.3: la 10.0.3 falla con `MSB3552` bajo el SDK 10.0.400 de la imagen Docker.
> - Se añadió BOM UTF-8 a los tres `.ps1`: sin él, Windows PowerShell 5.1 los lee como ANSI y destroza los caracteres no ASCII.
> - Se corrigieron las queries de Loki de `Observability.md`, que usaban una etiqueta (`service_name`) que no existe: los logs van de Serilog directo a Loki con la etiqueta `app`.
> - `xunit 2.9.2` aparece como *deprecated* (Legacy, sucedido por xunit.v3). Se documentó como excepción conocida en el checklist de TI; migrar la suite queda fuera de alcance.
> **Alcance revisado:** `README.md`, `docs/`, `deploy/`, `dev/`, `.github/`, el código de las 6 bibliotecas y los 4 proyectos de test, más build y ejecución de la suite.
>
> Las 10 decisiones abiertas quedaron resueltas en la § 3. Todas se cerraron con la opción recomendada, así que **todo lo que este plan marcaba como opcional pasa a estar aprobado**.

---

## 0. Resumen ejecutivo

La plantilla está **sólida a nivel de código y arquitectura**. Lo verificado con evidencia:

| Verificación | Resultado |
|---|---|
| `dotnet build -c Debug` | ✅ 0 errores (12 warnings, todos `NU1903` — ver H1) |
| `dotnet test --filter "Category!=RealDb"` | ✅ **197/197 verdes** (121 unit + 17 arquitectura + 59 integración) — coincide con lo que afirma el README |
| Suite RealDb | 6 tests declarados con `[Trait("Category","RealDb")]` — coincide con el README (no ejecutada en esta revisión) |
| Dirección de dependencias | ✅ Protegida por 17 architecture tests |
| `TODO` / `FIXME` / `HACK` en el código | ✅ Ninguno |

Los problemas encontrados son de **tres tipos**:

1. **Un bloqueante real de CI** (vulnerabilidades transitivas → el pipeline hoy fallaría).
2. **Documentación desactualizada o contradictoria** respecto al código real (rutas muertas, carpetas que no existen, número de proyectos, archivos citados que no están).
3. **La brecha principal que motivó esta revisión:** no existe guía de "clonar en un equipo nuevo", ni de "renombrar la plantilla", ni de "apuntar a otro repositorio", ni una guía completa de contenedores. Lo que hay en el README son 6 líneas superficiales que **no alcanzan**: 219 archivos versionados contienen el token `Company.Product`.

---

## 1. Hallazgos

### 1.1 Bloqueante

#### H1 — El CI fallaría hoy: 3 vulnerabilidades transitivas de severidad alta

`.github/workflows/ci.yml` compila con `/p:TreatWarningsAsErrors=true`, y NuGet emite `NU1903` como warning ⇒ **se convierte en error**. Comprobado localmente:

```
dotnet build /p:TreatWarningsAsErrors=true   →  6 Errores
```

| Paquete transitivo | Versión actual | Llega por | Advisory |
|---|---|---|---|
| `Microsoft.OpenApi` | 2.4.1 | `Swashbuckle.AspNetCore 10.1.4` | GHSA-v5pm-xwqc-g5wc (High) |
| `SSH.NET` | 2024.2.0 | `Testcontainers.MsSql 4.7.0` | GHSA-q939-rpr3-3284 (High) |
| `SQLitePCLRaw.lib.e_sqlite3` | 2.1.11 | `Microsoft.EntityFrameworkCore.Sqlite 10.0.3` | GHSA-2m69-gcr7-jv3q (High) |

Además invalida dos afirmaciones del `README.md` y del checklist de aprobación de TI:

- «`dotnet list package --vulnerable` → vacío» — **hoy es falso**.
- «`dotnet build --warnaserror` → 0 warnings, 0 errores» — **hoy es falso**, y además el flag no existe en `dotnet build` (debe ser `/warnaserror` o `/p:TreatWarningsAsErrors=true`).

**Resolución (C1) — decisión D-A: subir los paquetes padre.**

- `Swashbuckle.AspNetCore` 10.1.4 → 10.2.3
- `Testcontainers.MsSql` 4.7.0 → 4.14.0
- Si `Microsoft.EntityFrameworkCore.Sqlite 10.0.3` sigue arrastrando `SQLitePCLRaw.lib.e_sqlite3 2.1.11` (no hay versión padre más reciente en el tren 10.0.x), se fija **solo ese** transitivo activando `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>` y declarando la versión parcheada. Queda comentado en el `.props` para que se revise cuando EF Core publique una versión que ya lo incluya.

Criterio de cierre: `dotnet build /p:TreatWarningsAsErrors=true` sin errores, `dotnet list package --vulnerable --include-transitive` vacío y `dotnet test --filter "Category!=RealDb"` con 197 verdes. Si al subir `Testcontainers.MsSql` 4 versiones mayores algo se rompe en la suite RealDb, paro y te reporto antes de seguir.

---

### 1.2 Brechas de documentación (el motivo de la revisión)

#### H2 — No existe guía de puesta en marcha en un equipo nuevo

El README tiene un "arranque rápido en 5 pasos", pero **omite prerrequisitos que rompen el paso 4** en una máquina limpia:

- `dotnet-ef` no viene con el SDK y **no hay `.config/dotnet-tools.json`** en el repo ⇒ `dotnet ef database update` falla con *"Could not execute because the specified command or file was not found"*.
- `dotnet dev-certs https --trust` no se menciona, y tanto el perfil `https` de `launchSettings.json` como `app.UseHttpsRedirection()` lo necesitan.
- No se menciona Git, ni Docker Compose v2 (`docker compose`, sin guion), ni WSL2 como requisito de Docker Desktop en Windows.
- No hay tabla de troubleshooting (puerto 1433 ocupado, contenedor `unhealthy`, `Jwt:Key` corto, Mapperly sin IntelliSense hasta el primer build, etc.).

#### H3 — La guía de renombrado es insuficiente

El README resuelve el renombrado en 6 líneas. La realidad medida en el repo:

| Token | Dónde aparece |
|---|---|
| `Company.Product` | **219 archivos versionados** (namespaces, `using`, `.csproj`, `.slnx`, migraciones, tests, docs) |
| `company-product` | `appsettings.Development.json.example` (label de Loki), `README.md`, `deploy/database/docker-compose.sqlserver.yml` (`container_name`), `deploy/observability/docker-compose.observability.yml` (`name:` del proyecto compose), `deploy/observability/grafana/provisioning/datasources/datasources.yml`, `dev/database.ps1` |
| `CompanyProduct` | `appsettings.Development.json.example` (nombre de BBDD `CompanyProductDev`) |

Puntos que el README **no menciona** y que ensucian o rompen el proyecto derivado:

- Nombres de contenedor y de proyecto compose (`company-product-sqlserver`, `name: company-product-observability`) → colisionan si dos proyectos derivados corren a la vez en la misma máquina.
- Puertos fijos (1433, 3000, 3100, 3200, 4317, 4318, 8888, 8889, 9090, 5070, 7297) → misma colisión.
- Email del admin semilla (`admin@company.com`) en `IdentityDataSeeder`.
- `MigrationsHistoryTable("__EFMigrationsHistory", "identity")` y el schema `identity` del módulo.
- `OpenTelemetry:ServiceName` (aparece en las queries de Loki/Tempo ya documentadas).
- Nombres de assembly hardcodeados en `tests/Company.Product.ArchitectureTests/CleanArchitectureTests.cs`.
- El propio `.http`, `launchSettings.json` y el nombre de la carpeta raíz.
- La decisión de **conservar o regenerar** las 4 migraciones EF.

#### H4 — No existe guía para apuntar a un repositorio nuevo

Nada documenta: crear el repositorio destino, elegir entre historia limpia (`rm -rf .git` + `git init`) o historia conservada (`git remote set-url`), configurar `main`/`develop`, protección de ramas, y qué revisar en `.github/workflows/ci.yml` para la organización destino.

#### H5 — La documentación de contenedores está incompleta

Hoy solo hay contenedores para **dependencias** (SQL Server + stack de observabilidad). **No existe `Dockerfile` para la API**, ni `.dockerignore`, ni un compose que levante API + BBDD juntas. Para una plantilla que se desplegará en varios proyectos es una ausencia notable, y el `ReleaseChecklist.md` ya habla de "el artefacto/container", que hoy no existe.

---

### 1.3 Inconsistencias entre documentación y código real

| # | Archivo | Dice | Realidad |
|---|---|---|---|
| H6 | `docs/Observability.md` | `docker compose -f docker/observability/docker-compose.yml up -d` | **Ruta muerta.** Es `deploy/observability/docker-compose.observability.yml`; el camino recomendado es `dev/observability.ps1 up`. La Fase D.2 del plan 06 detectó esto y quedó a medias |
| H7 | `docs/Observability.md` | Lista puertos de Grafana y Collector | No menciona Loki (3100), Tempo (3200) ni Prometheus (9090), aunque más abajo documenta queries de Loki |
| H8 | `deploy/observability/README.md` | Tabla con 4 componentes | El compose levanta **5**: falta Loki (`localhost:3100`), que sí está en el compose y en los datasources de Grafana |
| H9 | `docs/01-Arquitectura.md` §2 | «Cada módulo tiene **4 proyectos**: `Domain`, `Application`, `Infrastructure`» | Son **3** (el propio texto lista 3) |
| H10 | `docs/01-Arquitectura.md` §2 | Árbol de `tests/` con 3 proyectos | Son **4**: falta `WebApi.IntegrationTests.RealDb` |
| H11 | `docs/Standards.md` — "Cómo agregar un nuevo módulo" | «Crear **cuatro** proyectos» | Son **3** |
| H12 | `docs/Standards.md` — "Cómo agregar un permiso" | «Crear la constante en `*.Infrastructure/Authorization/Permissions.cs`» | **Ese archivo no existe.** Los permisos son strings literales en controllers y seeder |
| H13 | `docs/02-Instrucciones-IA.md` §3.4 | «Constantes centralizadas en `Modules.<X>.Infrastructure/Authorization/<X>Permissions.cs`» | Mismo caso que H12 |
| H14 | `docs/02-Instrucciones-IA.md` §2.3 y §3.2 | Casos de uso en `Application/Features/<Feature>`; namespace `...Application.Features.Users.Commands.CreateUser` | La estructura real es `Application/Commands/<Caso>/` y `Application/Queries/<Recurso>/<Caso>/`. **No existe ninguna carpeta `Features`** |
| H15 | `docs/02-Instrucciones-IA.md` §2.3 | Excepciones de negocio en `Modules.<X>.Domain/Exceptions` | No existe esa carpeta; todas viven en `BuildingBlocks.SharedKernel/Exceptions` |
| H16 | `docs/Standards.md` — "Dónde poner qué" | Validators en `*.Application/Validators/` | Contradice a la sección "Cómo añadir un Command" **del mismo documento** (validator junto al command), que es lo que hace el código |
| H17 | `docs/05-Plan-Definitivo.md` | Enlaza `03-Plan-Implementacion.md` y `04-Analisis-Critico.md` | Ambos fueron eliminados ⇒ **2 enlaces rotos** |
| H18 | `docs/06-Plan-HandOff.md` | Plan de trabajo con decisiones "preguntar al usuario" ya resueltas; referencia `docs/README.md`, `03-` y `04-` | Documento **ya ejecutado**; su propia sección D.1 dice «eliminar tras aprobación de TI». Hoy un lector nuevo no distingue plan de estado actual |
| H19 | `README.md` | «`dotnet build --warnaserror`» en el checklist de TI | Flag inválido; debe ser `/warnaserror` o `/p:TreatWarningsAsErrors=true` |

---

### 1.4 Hallazgos de código (no bloqueantes, pero relevantes para una plantilla que va a crecer)

#### H20 — `IExecutionContext` colisiona en cuanto exista un segundo módulo ⚠️

`AddIdentityInfrastructure` registra:

```csharp
services.AddScoped<IExecutionContext, IdentityExecutionContext>();
```

`IExecutionContext` vive en `BuildingBlocks.SharedKernel` y es **global**. Cuando se agregue `AddCatalogInfrastructure` con su propio `CatalogExecutionContext`, **la última registración gana** y todos los handlers —incluidos los de Identity— recibirán el `DbContext` equivocado. `CommitAsync()` no lanzaría error: simplemente **no persistiría nada**, en silencio.

Es exactamente el escenario que la plantilla promete soportar, así que conviene resolverlo **antes** del primer proyecto derivado.

**Resolución — decisión D-D: refactor + architecture test (opción B2).** Se define `IIdentityExecutionContext : IExecutionContext` en `Application/Abstractions`, se registra esa interfaz en `AddIdentityInfrastructure`, se actualiza la inyección de los handlers que hoy piden `IExecutionContext`, y se añade un architecture test que falle si un módulo registra `IExecutionContext` desnudo. Así el ejemplo que copiarán todos los módulos futuros es el correcto. Se descartan B1 (solo documentar) y B3 (keyed services).

#### H21 — Los architecture tests no cubren módulos nuevos

`CleanArchitectureTests` fija los assemblies de Identity en constantes. Un módulo `Catalog` nuevo **no quedaría cubierto** por las reglas de dirección de dependencias (solo por el test genérico de aislamiento entre módulos, que sí descubre assemblies por convención). Propuesta: generalizar por convención de nombre (`Company.Product.Modules.*.Domain`, etc.) o documentar explícitamente que hay que añadir cada módulo a las constantes.

#### H22 — `Infrastructure/Authorization/IPermissionService.cs` es un residuo de refactor

El archivo no declara ninguna interfaz: contiene solo un `global using` alias hacia `Application.Abstractions.IPermissionService`. Un nombre de archivo que miente sobre su contenido. Propuesta: eliminarlo y poner el `using` normal en los archivos que lo necesiten.

#### H23 — No hay clase de constantes de permisos

Los permisos son strings literales en 16 puntos de los controllers y repetidos en `IdentityDataSeeder`. Un typo (`users.wirte`) no falla en compilación y produce un 403 silencioso. Los docs ya afirman que la clase existe (H12/H13). Propuesta: crear `IdentityPermissions.cs` con las 6 constantes y usarlas en controllers y seeder; o, si prefieres no tocar código, corregir los docs.

#### H24 — Faltan archivos de higiene esperables en una plantilla

- `.editorconfig` — formato consistente entre VS / VS Code / Rider y para `dotnet format`.
- `.config/dotnet-tools.json` con `dotnet-ef` fijado a la versión de EF Core (10.0.3) ⇒ `dotnet tool restore` reproducible.
- `Dockerfile` + `.dockerignore` (ver H5).

#### H25 — Cambios locales sin commitear

`Company.Product.WebApi/Properties/launchSettings.json` tiene un cambio no versionado (`launchBrowser: true` en el perfil `http`), y `docs/EntenderObservabilidad.md` (62 KB) está sin versionar y sin enlazar desde ningún índice. Hay que decidir qué se hace con ambos.

---

## 2. Cambios propuestos

### 2.1 Documentos nuevos

#### N1 — `docs/03-Guia-Nuevo-Equipo.md` *(cubre H2 y H5)*

Guía paso a paso, copy-paste, para quien clona por primera vez. Índice propuesto:

1. **Prerrequisitos** — tabla herramienta / versión / comando de verificación / cómo instalar: `git`, .NET SDK 10, `dotnet tool restore` (o `dotnet tool install -g dotnet-ef --version 10.0.3`), Docker Desktop (+ WSL2 en Windows), Docker Compose v2, y `dotnet dev-certs https --trust`.
2. **Paso 0 — clonar y verificar el toolchain** (`dotnet --version`, `docker compose version`).
3. **Paso 1 — secretos locales**: copiar `appsettings.Development.json.example`, qué significa cada campo, por qué `appsettings.Development.json` está en `.gitignore`, y la alternativa con `dotnet user-secrets`.
4. **Paso 2 — contenedores.** Sección explícita "qué contenedores necesito y cuáles no":

   | Contenedor | ¿Obligatorio? | Cómo se crea | Puertos | Verificación |
   |---|---|---|---|---|
   | SQL Server 2022 | Sí (salvo LocalDB o instancia propia) | `./dev/database.ps1 up` | 1433 | estado `healthy` en `docker ps` |
   | OTel Collector | No | `./dev/observability.ps1 up` | 4317 / 4318 / 8888 / 8889 | logs del collector |
   | Tempo | No | idem | 3200 | `http://localhost:3200/ready` |
   | Loki | No | idem | 3100 | `http://localhost:3100/ready` |
   | Prometheus | No | idem | 9090 | UI |
   | Grafana | No | idem | 3000 | UI (acceso anónimo con rol Admin) |
   | SQL Server efímero (Testcontainers) | Solo para `Category=RealDb` | automático al correr los tests | puerto aleatorio | — |

   Incluye qué hace cada comando por debajo, volúmenes creados, cómo borrar datos (`down -v` / `wipe`) y **cómo cambiar puertos** si están ocupados.
5. **Paso 3 — migraciones**: `dotnet ef database update ...`, qué crea (schema `identity`, tabla de historial) y qué hacer si falla.
6. **Paso 4 — ejecutar**: perfiles `http`, `https` y `Local+OTel`; qué URL abrir en cada caso.
7. **Paso 5 — validación funcional**: login admin, 2 endpoints, `/health/ready` y `/_info`, con la respuesta esperada de cada uno.
8. **Paso 6 — tests**: suite rápida, RealDb y cobertura.
9. **Configurar el IDE**: Visual Studio 2026 / VS Code (C# Dev Kit + REST Client) / Rider, y la nota de Mapperly (compilar antes de que aparezca el IntelliSense de los mappers generados).
10. **Troubleshooting** — tabla síntoma → causa → solución: `dotnet ef` no encontrado, 1433 ocupado, contenedor `unhealthy`, `Login failed for user 'sa'`, `Jwt:Key` < 32 chars, error de certificado en https, Docker sin WSL2, tests RealDb con Docker apagado, warnings `NU1903`.
11. **Checklist "listo para trabajar"** (10 casillas verificables).

#### N2 — `docs/04-Guia-Renombrado-y-Repositorio.md` *(cubre H3 y H4)*

1. **Elegir el nombre** — convención `<Empresa>.<Producto>` y sus derivados: `PascalCase` (`Acme.Inventory`), `kebab-case` (`acme-inventory`), sin puntos (`AcmeInventory`), nombre de BBDD y schema.
2. **Inventario completo de tokens** — la tabla de H3, con el comando de verificación final (`git grep -l "Company\.Product" | wc -l` → debe dar `0`).
3. **Procedimiento manual paso a paso**, ordenado para no romper el build a mitad:
   1. Cerrar el IDE; borrar `bin/`, `obj/`, `.vs/`.
   2. Reemplazo de contenido `Company.Product` → nombre nuevo en archivos de texto.
   3. Renombrar carpetas y `.csproj`.
   4. Renombrar el `.slnx` y actualizar sus rutas.
   5. Tokens en minúscula: contenedores, proyecto compose, labels de Loki, datasources de Grafana, scripts `dev/*.ps1`.
   6. Identidad de negocio: email del admin semilla, `Jwt:Issuer` / `Jwt:Audience`, `OpenTelemetry:ServiceName`, nombre de BBDD, schema del módulo.
   7. Puertos, si el proyecto va a convivir con otros derivados en la misma máquina.
   8. Decisión sobre migraciones: conservar las 4 actuales (recomendado, ya validadas) o regenerarlas desde cero.
   9. Verificación: `dotnet restore && dotnet build && dotnet test --filter "Category!=RealDb"` → 197 verdes.
4. **Script asistido** — `dev/rename.ps1 -NewName "Acme.Inventory" [-WhatIf]` (ver N4), con la ruta manual siempre disponible como respaldo.
5. **Repositorio nuevo** — dos caminos:
   - *Historia limpia* (recomendado para un producto nuevo): borrar `.git`, `git init`, primer commit «Base a partir de la plantilla Company.Product».
   - *Historia conservada*: `git remote set-url origin <nuevo>` o `git remote add`, y push al nuevo remoto; cuándo tiene sentido cada uno.
   Incluye: crear el repo en la organización, ramas `main` / `develop`, protección de rama, y qué revisar de `.github/workflows/ci.yml` (secrets, runners, y el bloque de Azure DevOps que ya viene comentado en el YAML por si la organización usa ADO).
6. **Checklist post-renombrado**: build, tests, arranque, Swagger, login, Grafana con el nuevo `ServiceName`, `git grep` sin residuos.
7. **Qué NO renombrar**: `__EFMigrationsHistory`, ids de paquetes NuGet, la clase `Program`, etc.

#### N3 — `docs/README.md` (índice de documentación) *(aprobado; mitiga H18)*

Página corta que indique, por rol (dev nuevo / arquitecto / revisor de TI / agente IA), qué documento leer y en qué orden. Evita que alguien abra `06-Plan-HandOff.md` creyendo que describe el estado actual.

#### N4 — `dev/rename.ps1` *(aprobado por D-C; automatiza N2)*

Script con `-WhatIf` que valida el nombre nuevo, reemplaza el contenido en archivos versionados de texto, renombra carpetas / `.csproj` / `.slnx`, cubre los tres formatos del token y termina imprimiendo el checklist de verificación. **No** toca `.git` (esa decisión queda en el usuario). Es código nuevo, requiere tu aprobación.

#### N5 — `Dockerfile` + `.dockerignore` + `deploy/app/docker-compose.yml` *(aprobado por D-B; cubre H5)*

- `Dockerfile` multi-stage (SDK 10 → runtime ASP.NET 10), usuario no-root, `COMMIT_SHA` / `BUILD_DATE` como `ARG`→`ENV` (ya los consume `/_info`) y `HEALTHCHECK` contra `/health/live`.
- `deploy/app/docker-compose.yml` que levante API + SQL Server, con la observabilidad como perfil opcional: demo o QA en un solo comando.
- `.dockerignore` para excluir `bin/`, `obj/`, `.git`, `docs/`.

#### N6 — `.config/dotnet-tools.json` y `.editorconfig` *(cubre H24 — ver nota de alcance)*

Manifiesto con `dotnet-ef` 10.0.3 (⇒ `dotnet tool restore` en lugar de instalación global) y un `.editorconfig` alineado con el estilo ya presente en el código.

> **Nota de alcance (no estaba entre las 10 decisiones).** Propongo separarlo en dos:
> - **`.config/dotnet-tools.json`: incluido.** La guía N1 se apoya en `dotnet tool restore`; sin el manifiesto la guía sería falsa y el dev nuevo tendría que instalar `dotnet-ef` a mano con la versión correcta adivinada.
> - **`.editorconfig`: fuera por ahora.** Es opinable y no bloquea nada. Si lo quieres dentro, dímelo en esta revisión y lo añado alineado al estilo actual del código.

---

### 2.2 Ediciones sobre documentos existentes

| # | Archivo | Cambio |
|---|---|---|
| E1 | `README.md` | Prerrequisitos: añadir `dotnet-ef`, `dotnet dev-certs https --trust`, Git y Compose v2 |
| E2 | `README.md` | Sección "Renombrar la plantilla": reducir a 3 líneas + enlace a `docs/04-Guia-Renombrado-y-Repositorio.md` |
| E3 | `README.md` | Enlace destacado a `docs/03-Guia-Nuevo-Equipo.md` al inicio del "Arranque rápido" |
| E4 | `README.md` | Checklist de TI: corregir `dotnet build --warnaserror` (H19) y volver a verificar el criterio de `--vulnerable`, ya vacío tras C1 |
| E5 | `README.md` | Tabla "Documentación de soporte": añadir los documentos nuevos |
| E6 | `docs/Observability.md` | Corregir la ruta muerta del compose (H6) y añadir la tabla de puertos completa con Loki / Tempo / Prometheus (H7) |
| E7 | `deploy/observability/README.md` | Añadir Loki a la tabla de componentes y a la verificación end-to-end (H8) |
| E8 | `docs/01-Arquitectura.md` | «4 proyectos» → 3 (H9); añadir `WebApi.IntegrationTests.RealDb` al árbol (H10) |
| E9 | `docs/Standards.md` | «cuatro proyectos» → 3 (H11); corregir la sección de permisos apuntando a `IdentityPermissions.cs` (H12); resolver la contradicción sobre validators (H16); añadir el patrón `I<Modulo>ExecutionContext` por módulo (H20) |
| E10 | `docs/02-Instrucciones-IA.md` | `Features/` → `Commands/` + `Queries/` (H14); excepciones en SharedKernel (H15); apuntar a `IdentityPermissions.cs` ya creado en C3 (H13); patrón `I<Modulo>ExecutionContext` de C2 (H20) |
| E11 | `docs/05-Plan-Definitivo.md` | Quitar o anotar los 2 enlaces rotos a `03-` y `04-` (H17) |
| E12 | `docs/06-Plan-HandOff.md` | Mover a `docs/archive/` con cabecera «EJECUTADO — documento histórico, no describe el estado actual» (H18, decisión D-F) |
| E13 | `docs/ReleaseChecklist.md` | Añadir que el admin semilla se crea con hash placeholder fuera de Development (hay que fijar la contraseña real en el primer deploy); y los ítems de la imagen de contenedor creada en N5 |

---

### 2.3 Cambios de código (aprobados en la § 3)

Todos aprobados en la ronda de decisiones del 2026-08-20.

| # | Cambio | Motivo | Riesgo | Estado |
|---|---|---|---|---|
| C1 | Resolver las 3 vulnerabilidades transitivas en `Directory.Packages.props` | H1 — el CI falla hoy | Bajo; se valida con los 197 tests | ✅ D-A |
| C2 | `IIdentityExecutionContext` + architecture test que prohíba registrar `IExecutionContext` genérico | H20 — bug silencioso al añadir el 2.º módulo | Medio: toca la inyección de todos los handlers | ✅ D-D |
| C3 | `IdentityPermissions.cs` con las 6 constantes, usadas en controllers y seeder | H23 — elimina strings mágicos y alinea el código con los docs | Bajo | ✅ D-E |
| C4 | Eliminar `Infrastructure/Authorization/IPermissionService.cs` (alias residual) | H22 | Bajo | ✅ |
| C5 | Generalizar `CleanArchitectureTests` por convención de nombre de assembly | H21 | Bajo | ✅ |
| C6 | Commitear `launchSettings.json` y versionar `docs/EntenderObservabilidad.md` | H25 | Nulo | ✅ D-G / D-H |

---

## 3. Decisiones tomadas (2026-08-20)

Las diez se resolvieron con la opción recomendada.

| # | Decisión | Resultado | Qué implica |
|---|---|---|---|
| **D-A** | Vulnerabilidades (H1) | **Subir paquetes padre** | Swashbuckle 10.2.3 y Testcontainers.MsSql 4.14.0; pinning transitivo solo si SQLitePCLRaw sigue vulnerable |
| **D-B** | Contenedor de la API (N5) | **Sí, completo** | `Dockerfile` multi-stage + `.dockerignore` + `deploy/app/docker-compose.yml` (API + SQL Server, observabilidad como perfil) |
| **D-C** | Script de renombrado (N4) | **Sí, con `-WhatIf`** | `dev/rename.ps1`; la guía manual de N2 se mantiene como respaldo |
| **D-D** | `IExecutionContext` (H20) | **Refactor + architecture test** | `IIdentityExecutionContext` en Identity + test que prohíba registrar la interfaz genérica |
| **D-E** | Constantes de permisos (H23) | **Crear la clase** | `IdentityPermissions.cs` usada en los 16 puntos de controllers y en el seeder |
| **D-F** | `06-Plan-HandOff.md` (H18) | **Archivar** | Mover a `docs/archive/` con cabecera de documento histórico |
| **D-G** | `EntenderObservabilidad.md` (H25) | **Versionar e indexar** | Revisado: es una ruta de estudio de observabilidad, sin credenciales ni datos personales. Entra al repo y a la tabla de documentación del README |
| **D-H** | `launchSettings.json` local (H25) | **Commitear** | `launchBrowser: true` en el perfil `http` entra en el commit final |
| **D-I** | Idioma de los documentos nuevos | **Español** | Coherente con el resto de `docs/` y el README |
| **D-J** | Numeración | **`03-` y `04-`** | `docs/03-Guia-Nuevo-Equipo.md` y `docs/04-Guia-Renombrado-y-Repositorio.md` |

**Único punto abierto:** el `.editorconfig` de N6 (ver la nota de alcance en N6). Lo dejo fuera salvo que me digas lo contrario en esta revisión.

---

## 4. Orden de ejecución propuesto

1. **Fase 1 — Bloqueante:** C1 (vulnerabilidades) + verificación con `build /p:TreatWarningsAsErrors=true`, `list package --vulnerable` vacío y 197 tests verdes.
2. **Fase 2 — Código:** C2 (`IIdentityExecutionContext` + test), C3 (`IdentityPermissions`), C4 (borrar alias residual), C5 (architecture tests por convención). Va antes que la documentación **para que las guías describan el código final y no haya que reescribirlas**. Tras cada cambio: build + suite rápida verdes.
3. **Fase 3 — Contenedores y tooling:** N5 (`Dockerfile`, `.dockerignore`, `deploy/app/docker-compose.yml`) y `.config/dotnet-tools.json` de N6. Verificación real: `docker build` + `docker compose up` + login del admin contra la API en contenedor.
4. **Fase 4 — Documentos nuevos:** N1 (nuevo equipo) → N2 (renombrado y repositorio) → N4 (`dev/rename.ps1`, probado con `-WhatIf`) → N3 (índice).
5. **Fase 5 — Correcciones de docs:** E1–E13 (rápidas y mecánicas) + archivar `06-Plan-HandOff.md` (D-F) + versionar `EntenderObservabilidad.md` (D-G).
6. **Fase 6 — Cierre:** `dotnet build /p:TreatWarningsAsErrors=true`, suite rápida, suite RealDb, `dotnet list package --vulnerable`; actualizar el checklist de TI del README con los resultados reales; proponer el commit (**no lo ejecuto sin tu confirmación**).

**Estimación:** Fase 1 ~20 min · Fase 2 ~1 h 30 · Fase 3 ~1 h · Fase 4 ~2 h · Fase 5 ~45 min · Fase 6 ~20 min.

> **Puntos donde paro y te consulto durante la ejecución:** si subir `Testcontainers.MsSql` rompe la suite RealDb; si el refactor de `IExecutionContext` obliga a tocar más superficie de la prevista; y antes de cualquier `git rm` o commit.

---

## 5. Lo que NO se cambia

- Las decisiones D1–D10 de `05-Plan-Definitivo.md` (dispatcher propio, Mapperly, Shouldly, patrón B de repositorios, HybridCache, SQLite + Testcontainers).
- La estructura de carpetas ni la dirección de dependencias.
- Endpoints, contratos HTTP ni el esquema de BBDD.
- Añadir un segundo módulo de ejemplo.
- Publicar el repositorio o configurar un despliegue real.
