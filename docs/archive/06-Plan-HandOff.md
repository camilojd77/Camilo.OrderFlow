> ⚠️ **DOCUMENTO HISTÓRICO — EJECUTADO.** Este plan se completó durante el hand-off a TI.
> **No describe el estado actual de la plantilla** y sus decisiones abiertas («preguntar al usuario»)
> ya están resueltas. Para el estado actual: [`../README.md`](../README.md) y [`../03-Guia-Nuevo-Equipo.md`](../03-Guia-Nuevo-Equipo.md).
> Se conserva como evidencia de qué se revisó y por qué.

---

# Documento 6 — Plan de Hand-Off al Equipo de TI

> **Audiencia:** asistente de implementación (Sonnet 4.6) que dejará la plantilla `Company.Product` en estado **listo para revisión por el equipo de TI** de la compañía.
> **Estado:** las fases 0–10 del [`05-Plan-Definitivo.md`](../05-Plan-Definitivo.md) están ejecutadas. Falta una pasada de **operación, limpieza y cierre documental** para que un revisor externo pueda clonar, levantar y aprobar la plantilla sin sorpresas.
>
> **Premisas del entorno local del usuario:**
> - Docker Desktop está **corriendo**.
> - El stack de observabilidad ya está levantado vía `dev/observability.ps1 up` (OTel Collector + Tempo + Loki + Prometheus + Grafana en `deploy/observability/`).
> - Hay una instancia de SQL Server **alcanzable** (sea contenedor manual del usuario, LocalDB o instancia local). El connection string vive en `appsettings.Development.json` (no versionado).
>
> **Este documento NO modifica decisiones D1–D10 del `05`.** Si alguna tarea aquí requiere desviarse de esas decisiones, parar y preguntar.

---

## 1. Objetivo único de este plan

Dejar el repositorio en un estado donde un ingeniero externo (revisor de TI) pueda:

1. Clonar.
2. Copiar `appsettings.Development.json.example` → `appsettings.Development.json` y ajustar credenciales.
3. Correr `dev/observability.ps1 up` (opcional) y `dotnet ef database update`.
4. Correr `dotnet test --filter "Category!=RealDb"` y verlo verde.
5. Correr `dotnet run --project Company.Product.WebApi`, abrir Swagger, hacer login con el admin seed y ejercitar al menos 3 endpoints.
6. Leer **un único** documento de entrada (`README.md`) y entender en < 10 minutos qué es la plantilla, qué tiene, qué le falta y cómo extenderla.

Si cualquiera de esos pasos falla, **el hand-off no está completo**.

---

## 2. Reglas durante este plan

1. **No introducir librerías nuevas.** El stack está congelado por `05`.
2. **No reabrir decisiones D1–D10.**
3. **No tocar la lógica de negocio existente** salvo que un test esté roto.
4. **Versiones de NuGet solo en `Directory.Packages.props`.**
5. **Cualquier decisión arquitectónica fuera del alcance de este plan → parar y preguntar al usuario.**
6. Después de cada fase: `dotnet build /warnaserror` + `dotnet test --filter "Category!=RealDb"` deben quedar verdes.

---

## 3. Fases ordenadas

### Fase A — Verificación de entorno y migraciones

#### A.1 Build y restore desde cero

```pwsh
dotnet restore
dotnet build /warnaserror
```

- Si hay warnings nuevos, **arreglarlos** (no suprimirlos). Si un warning es legítimamente intencional, justificarlo y pedir confirmación.

#### A.2 Aplicar migraciones a la BD local

Asumir que el connection string `ConnectionStrings:DefaultConnection` ya apunta a una instancia de SQL Server alcanzable.

```pwsh
dotnet ef database update `
  --project Company.Product.Modules.Identity.Infrastructure `
  --startup-project Company.Product.WebApi
```

- Verificar que las 4 migraciones se aplicaron en orden:
  - `20260304152336_InitialIdentity`
  - `20260507202105_AddUserProfile`
  - `20260507203931_AddUserLockout`
  - `20260507203947_AddRefreshTokens`
- Si falla, **NO** reescribir migraciones. Reportar el error al usuario.

#### A.3 Levantar la API y validar boot

```pwsh
dotnet run --project Company.Product.WebApi
```

- `GET /_info` debe responder 200 con `commitSha` y `buildDate`.
- `GET /health/live` y `GET /health/ready` deben responder 200.
- El admin seed (`admin@company.com` / `Admin123!` por convención del seeder) debe poder hacer login y obtener un access token.
- Detener la API después de la validación.

> **Si el seeder falla porque ya hay datos:** OK, es idempotente. Si falla por otra razón, reportar.

---

### Fase B — Suite de tests completa

#### B.1 Run rápido (default — SQLite + unit + arquitectura)

```pwsh
dotnet test --filter "Category!=RealDb" --logger "console;verbosity=normal"
```

- Debe quedar **100% verde**. Si un test rojo, arreglarlo (test o código según diagnóstico). Si la causa raíz no es obvia, **parar y preguntar**.

#### B.2 Run RealDb (Testcontainers + SQL Server real)

```pwsh
dotnet test --filter "Category=RealDb" --logger "console;verbosity=normal"
```

- Requiere Docker corriendo (ya lo está).
- Si hay flakiness por arranque del contenedor, esperar y reintentar **una sola vez**. Si falla 2 veces, reportar.

#### B.3 Cobertura

```pwsh
dotnet test --filter "Category!=RealDb" --collect:"XPlat Code Coverage"
```

- Reportar al usuario el porcentaje aproximado total. **No** se exige umbral mínimo en esta plantilla, pero se documenta.

---

### Fase C — Limpieza de código muerto

> **Regla:** "muerto" significa **no referenciado por ningún `using`, no inyectado vía DI, no usado en tests, no parte de un contrato público de la plantilla**. Ante cualquier duda, **NO borrar** y reportar al usuario.

#### C.1 Inventario

Generar (en memoria, sin escribir a disco) una lista de:

1. Tipos `internal`/`public` no referenciados.
2. Métodos públicos sin call-sites.
3. `using` no utilizados — usar el analizador del compilador (`IDE0005`) o `dotnet format --severity info`.
4. Archivos de seguimiento de IDE no necesarios (`*.csproj.user`, `*.suo`, `.vs/`, `bin/`, `obj/`) — verificar que estén en `.gitignore` y, si no lo están, agregarlos.
5. Strings/constantes literales sin uso.

#### C.2 Aplicar limpieza

- Eliminar imports no usados con `dotnet format`.
- Eliminar tipos/métodos muertos **solo si no son contratos públicos de la plantilla** (las interfaces `IRequest`, `ISender`, `IExecutionContext`, etc. son parte del API pública aunque no estén usadas en el código de aplicación).
- **NO** eliminar permisos del seeder, aunque ningún endpoint los use todavía.
- **NO** eliminar campos de `appsettings.Development.json.example`, porque sirven como documentación.

#### C.3 Verificación

```pwsh
dotnet build /warnaserror
dotnet test --filter "Category!=RealDb"
```

Verde. Si rojo, revertir los borrados que rompieron y preguntar.

---

### Fase D — Limpieza y consolidación de documentación

#### D.1 Inventario de docs

Archivos en `docs/`:

| Archivo | Propósito | Decisión |
|---|---|---|
| `01-Arquitectura.md` | Visión arquitectónica | **Conservar** y verificar que sigue exacto al código real. |
| `02-Instrucciones-IA.md` | Convenciones para asistentes IA | **Conservar**. Si una IA va a seguir extendiendo la plantilla, este doc es útil. |
| `03-Plan-Implementacion.md` | Plan original (superseded) | **Eliminar** o mover a `docs/archive/`. Ya no aporta al revisor de TI. |
| `04-Analisis-Critico.md` | Justificación de cambios al `03` | **Eliminar** o mover a `docs/archive/`. |
| `05-Plan-Definitivo.md` | Plan ejecutado | **Conservar** como evidencia de qué se construyó. Marcar al inicio "EJECUTADO — fases 0..10 completas". |
| `06-Plan-HandOff.md` | Este documento | **Conservar** durante el hand-off; eliminar tras aprobación de TI. |
| `Observability.md` | Cómo usar Grafana/Loki/Tempo | **Conservar**. |
| `ReleaseChecklist.md` | Checklist de deploy | **Conservar**. |
| `Standards.md` | Convenciones, patrones de módulo | **Conservar**. |
| `README.md` (en `docs/`) | Guía de arranque local | **Consolidar con `README.md` raíz** — ver D.3. |

> **Decisión de archivado:** preferir `git rm` con commit explícito sobre `docs/archive/`. La historia git ya conserva el contenido. **Confirmar con el usuario antes de borrar.**

#### D.2 Corregir inconsistencias detectadas

1. `README.md` (raíz) y `docs/README.md` referencian `docker compose -f docker/observability/docker-compose.yml`. La ruta real es:
   - `deploy/observability/docker-compose.observability.yml` (compose file).
   - `dev/observability.ps1 up` (wrapper recomendado).
   Actualizar ambas menciones a la ruta real **y** documentar el wrapper como camino preferido.

2. `.gitignore` contiene `.github/` ⇒ el workflow `ci.yml` **no se está versionando**. Decisión:
   - Si la plantilla quiere CI activo: quitar `.github/` de `.gitignore`, hacer `git add .github/workflows/ci.yml`, commitear.
   - Si la plantilla deja CI a discreción del consumidor: documentar explícitamente en README "CI: configurar `.github/workflows/ci.yml` en tu organización" y mantener el `.gitignore`.
   - **Preguntar al usuario qué opción prefiere.** No decidir solo.

3. No existe `docker-compose.yml` para SQL Server. La plantilla solo tiene compose para observabilidad. Decisión:
   - **Recomendación:** agregar `deploy/database/docker-compose.sqlserver.yml` con un servicio `mssql` simple para desarrollo local + un wrapper `dev/database.ps1`. Esto evita que cada dev nuevo tenga que improvisar.
   - **Alternativa:** documentar en README un comando `docker run` autocontenido.
   - **Preguntar al usuario** qué opción prefiere antes de crear archivos nuevos.

#### D.3 Consolidar `README.md` raíz como puerta única

El revisor de TI debe poder leer **un solo archivo** y saber qué hacer. Reescribir `README.md` (raíz) para que cubra, en este orden:

1. **Qué es** (1 párrafo).
2. **Características incluidas** (tabla — ya existe, mantener).
3. **Prerrequisitos** (`.NET 10 SDK`, Docker Desktop, SQL Server alcanzable).
4. **Arranque rápido en 5 pasos** con comandos copy-paste:
   1. Clonar.
   2. Copiar `appsettings.Development.json.example` → `appsettings.Development.json` y ajustar.
   3. Levantar BD + observabilidad (con los comandos correctos resueltos en D.2).
   4. `dotnet ef database update ...`.
   5. `dotnet run --project Company.Product.WebApi`.
5. **Validación rápida** (curl o bloque `.http`):
   - login admin → access token.
   - `GET /api/users/me` con el token.
   - `GET /api/users/me/permissions`.
6. **Tests** (rápido + RealDb).
7. **Estructura del repositorio** (árbol resumido de carpetas top-level con 1 línea cada una).
8. **Próximos pasos / cómo extender** — link a [`docs/Standards.md`](../Standards.md) y [`docs/02-Instrucciones-IA.md`](../02-Instrucciones-IA.md).
9. **Checklist de aprobación TI** (tabla con criterios objetivos verificables — ver § 4).
10. **Documentación de soporte** (tabla con links a `docs/`).
11. **Licencia / propiedad**.

> **`docs/README.md` se elimina** tras consolidar su contenido en el README raíz.

#### D.4 Verificar que `01-Arquitectura.md` refleja el código

Recorrer `01-Arquitectura.md` con el código en la mano:

- Diagramas / referencias a paquetes deben citar Mapperly, Shouldly, dispatcher propio (no MediatR/AutoMapper).
- Lista de módulos debe ser exactamente `Identity` (único módulo hoy).
- Tabla de "qué va dónde" debe coincidir con la estructura real de carpetas.

Si hay desfase, **corregir el doc** (no el código).

#### D.5 Verificar que `02-Instrucciones-IA.md` refleja convenciones reales

- Sección de patrones: mencionar dispatcher propio (`ISender`), Mapperly, Shouldly, NSubstitute. Eliminar cualquier mención remanente a MediatR/AutoMapper/FluentAssertions/Moq.
- Tabla de archivos por capa: debe coincidir con `01`.

#### D.6 Actualizar `Standards.md` y `ReleaseChecklist.md`

- `Standards.md`: confirmar que la sección "patrón completo de módulo" es ejecutable (un dev puede seguirla y agregar un módulo nuevo). Si falta algo (ej. cómo invalidar cache cross-módulo), agregarlo.
- `ReleaseChecklist.md`: revisar que cada bullet es verificable con un comando o un check de UI. Si hay bullets ambiguos, reescribir.

---

### Fase E — Smoke test E2E manual (asistido)

> **Esta fase la corre el asistente, pero los resultados los reporta al usuario para que él valide.**

1. Levantar API local.
2. Ejecutar la siguiente secuencia con `Company.Product.WebApi.http` (o `curl`):
   - `POST /api/auth/login` (admin) → captura `accessToken`.
   - `GET /api/users/me` → 200 con perfil del admin.
   - `GET /api/users/me/permissions` → contiene los 6 permisos del seed.
   - `POST /api/permissions` con `{"code":"reports.read","name":"Ver reportes"}` → 201.
   - `POST /api/roles` con `{"name":"Auditor","description":"..."}` → 201, captura `roleId`.
   - `PUT /api/roles/{roleId}/permissions` con el permiso recién creado → 204.
   - `POST /api/users` con un usuario nuevo → 201, captura `userId`.
   - `PUT /api/users/{userId}/roles` asignando `roleId` → 204.
   - Login con el nuevo usuario → captura su `accessToken`.
   - `GET /api/users/me/permissions` con el nuevo token → contiene `reports.read`.
3. Reportar el resultado de cada paso al usuario (✅ o ❌ con error).
4. Si todo verde: tag mental "smoke E2E pasa".

---

### Fase F — Cierre y reporte de hand-off

#### F.1 Verificación final

```pwsh
dotnet restore
dotnet build /warnaserror
dotnet test --filter "Category!=RealDb"
dotnet test --filter "Category=RealDb"
dotnet list package --vulnerable
dotnet list package --deprecated
```

- Sin warnings, sin tests rojos, sin paquetes vulnerables ni deprecados.
- Si aparece algún paquete vulnerable: **NO actualizar sin permiso**. Reportar el CVE y la versión recomendada al usuario.

#### F.2 Reporte final al usuario

Producir un reporte breve (≤ 200 palabras) con:

- Estado de cada fase A–F (✅/❌).
- Archivos eliminados, archivos creados, archivos modificados (conteos + listas).
- Decisiones consultadas durante la ejecución y respuesta del usuario.
- Issues abiertos / TODOs residuales (si los hay).
- Recomendación: **listo para enviar a TI** o **bloqueado por X**.

#### F.3 Commit final propuesto

Mensaje sugerido (en español, conciso):

```
Hand-off: limpieza, consolidación de README y verificación de fases 0–10

- Consolida la guía de arranque en README.md raíz; elimina docs/README.md.
- Archiva planes históricos (03-Plan-Implementacion.md, 04-Analisis-Critico.md).
- Corrige rutas de docker-compose en README y docs.
- (Otros cambios concretos…)

Tests: dotnet test --filter "Category!=RealDb" verde (X/X).
       dotnet test --filter "Category=RealDb" verde (Y/Y).
```

> **NO** crear el commit sin confirmación explícita del usuario.

---

## 4. Checklist de aprobación TI (criterios objetivos)

Estos criterios son los que el revisor de TI usará. Si todos pasan, la plantilla queda aprobada.

- [ ] `git clone` + copiar `appsettings.Development.json.example` + `dotnet ef database update` + `dotnet run` arranca la API en < 5 minutos en una máquina sin estado previo.
- [ ] El admin seed funciona: login devuelve access token + cookie `refresh_token`.
- [ ] `dotnet test --filter "Category!=RealDb"` ⇒ verde.
- [ ] `dotnet test --filter "Category=RealDb"` ⇒ verde con Docker corriendo.
- [ ] `dotnet build /warnaserror` ⇒ sin warnings.
- [ ] `dotnet list package --vulnerable` ⇒ vacío.
- [ ] `dotnet list package --deprecated` ⇒ vacío (o documentado).
- [ ] Cada endpoint público está documentado en Swagger con `[ProducesResponseType]` y autenticación correcta.
- [ ] `Architecture Tests` cubren todas las reglas de Clean Architecture y aislamiento entre módulos.
- [ ] `README.md` raíz cubre los puntos 1–11 de § D.3 sin ambigüedad.
- [ ] `appsettings.Development.json.example` permite a un dev nuevo arrancar sin tener que adivinar campos.
- [ ] No hay archivos de IDE (`*.user`, `.vs/`, etc.) ni `bin/`/`obj/` versionados.
- [ ] Las 4 migraciones EF aplican limpiamente sobre una BD vacía.
- [ ] Smoke E2E (Fase E) pasa.

---

## 5. Lo que NO se hace en este plan

- No se agregan endpoints nuevos.
- No se cambia la decisión sobre Result vs Excepciones.
- No se introduce un segundo módulo (eso lo hará el equipo consumidor).
- No se publica el repositorio a un remoto público.
- No se configura un pipeline de despliegue real.

Si algo de esto aparece como necesario durante la ejecución, **parar y preguntar**.
