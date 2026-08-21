# Documento 3 — Puesta en marcha en un equipo nuevo

> **Audiencia:** desarrollador que acaba de clonar el repositorio y necesita dejarlo corriendo.
> **Objetivo:** de `git clone` a la API funcionando con login del admin, sin adivinar nada.
> **Tiempo estimado:** 15–25 minutos la primera vez (la mayor parte es descarga de imágenes Docker).

Si lo que quieres es **adaptar la plantilla a un proyecto nuevo** (otro nombre, otro repositorio), esa es otra guía: [`04-Guia-Renombrado-y-Repositorio.md`](04-Guia-Renombrado-y-Repositorio.md). Primero deja el proyecto corriendo con esta.

---

## 1. Prerrequisitos

| Herramienta | Versión | Verificar con | Obligatorio |
|---|---|---|---|
| Git | 2.40+ | `git --version` | Sí |
| .NET SDK | **10.0** | `dotnet --version` | Sí |
| Docker Desktop | 4.x (Compose v2) | `docker compose version` | Sí, salvo que uses una instancia propia de SQL Server |
| SQL Server | 2019+ | — | Sí (contenedor, LocalDB o instancia de red) |
| Certificado HTTPS de desarrollo | — | `dotnet dev-certs https --check` | Solo si vas a usar el perfil `https` |

### 1.1 .NET SDK

Descarga desde <https://dotnet.microsoft.com/download/dotnet/10.0>. Después:

```pwsh
dotnet --version
```

Debe imprimir `10.x`. Si imprime 8 o 9, tienes un SDK viejo en el PATH.

### 1.2 Docker Desktop (Windows)

Requiere **WSL2** activo. Si Docker Desktop no arranca:

```pwsh
wsl --status
wsl --update
```

Comprueba que el daemon responde antes de seguir:

```pwsh
docker compose version
```

> Si `docker` responde pero `docker compose` no, tienes Compose v1 (`docker-compose`, con guion). Todos los comandos de este repositorio usan **v2**.

### 1.3 Herramientas .NET del repositorio (`dotnet ef`)

`dotnet ef` **no viene con el SDK**. No hace falta instalarlo a mano: el repositorio trae un manifiesto en `.config/dotnet-tools.json` con la versión exacta.

```pwsh
dotnet tool restore
dotnet ef --version
```

> **Por qué manifiesto y no instalación global:** todos los desarrolladores y el contenedor `migrator` usan la misma versión, y actualizarla es un cambio versionado en git. Si actualizas EF Core en `Directory.Packages.props`, revisa este manifiesto: la herramienta debe ser **igual o más nueva** que el runtime de EF Core.

### 1.4 Certificado HTTPS de desarrollo

Solo si vas a arrancar con el perfil `https` (o desde Visual Studio, que lo usa por defecto):

```pwsh
dotnet dev-certs https --trust
```

Acepta el diálogo de confianza de Windows. Sin esto el navegador rechaza `https://localhost:7297`.

---

## 2. Paso 0 — Clonar

```pwsh
git clone <url-del-repo>
cd Company.Product
dotnet --version
docker compose version
```

---

## 3. Paso 1 — Configuración local

`appsettings.json` **no trae secretos**: los campos sensibles están vacíos a propósito. Cada desarrollador crea su propio `appsettings.Development.json`, que está en `.gitignore` y nunca se commitea.

```pwsh
Copy-Item Company.Product.WebApi\appsettings.Development.json.example `
          Company.Product.WebApi\appsettings.Development.json
```

Abre la copia y ajusta como mínimo:

| Campo | Qué poner |
|---|---|
| `ConnectionStrings:DefaultConnection` | Tu instancia de SQL Server (ver paso 2). El valor de ejemplo apunta al contenedor de `dev/database.ps1` |
| `ConnectionStrings:Db` | El **mismo** valor: lo usa el health check de readiness |
| `Jwt:Key` | Mínimo **32 caracteres**. La app no arranca si es más corta (`ValidateOnStart`) |

El resto del archivo ya viene con valores razonables: seed activado, Serilog hacia Loki, OpenTelemetry apuntando al collector local, CORS para los puertos habituales de front (3000, 4200, 5173).

> **Alternativa sin archivo:** si prefieres no tener secretos en disco dentro del repo, usa User Secrets:
> ```pwsh
> dotnet user-secrets --project Company.Product.WebApi set "Jwt:Key" "una-clave-de-al-menos-32-caracteres"
> ```

---

## 4. Paso 2 — Contenedores

La plantilla usa contenedores para **dependencias**, no para la app (durante el desarrollo la API corre con `dotnet run`). Solo el primero es obligatorio.

| Contenedor | ¿Obligatorio? | Cómo se crea | Puertos | Para qué |
|---|---|---|---|---|
| SQL Server 2022 | **Sí** (salvo instancia propia) | `.\dev\database.ps1 up` | 1433 | Base de datos de la aplicación |
| OTel Collector | No | `.\dev\observability.ps1 up` | 4317, 4318, 8888, 8889 | Recibe trazas y métricas de la API |
| Grafana Tempo | No | idem | 3200 | Almacena trazas |
| Grafana Loki | No | idem | 3100 | Almacena logs |
| Prometheus | No | idem | 9090 | Almacena métricas |
| Grafana | No | idem | 3000 | Visualiza todo lo anterior |
| SQL Server efímero | Solo para tests `RealDb` | Automático (Testcontainers) | puerto aleatorio | Tests contra SQL Server real |

### 4.1 Base de datos

```pwsh
.\dev\database.ps1 up      # levanta y espera a que esté healthy (~30 s la primera vez)
.\dev\database.ps1 logs    # ver logs
.\dev\database.ps1 down    # parar (los datos persisten en el volumen)
```

Deja SQL Server en `localhost:1433` con `sa` / `Password123!` — credenciales **solo de desarrollo local**. Por debajo es `deploy/database/docker-compose.sqlserver.yml`.

Si prefieres LocalDB o una instancia de red, salta este paso y ajusta el connection string del paso 1.

### 4.2 Observabilidad (opcional)

```pwsh
.\dev\observability.ps1 up       # levanta los 5 contenedores
.\dev\observability.ps1 status   # estado de cada uno
.\dev\observability.ps1 logs     # logs combinados
.\dev\observability.ps1 down     # parar (los datos persisten)
.\dev\observability.ps1 wipe     # parar y BORRAR los volúmenes
```

La primera vez descarga ~600 MB de imágenes. Detalle de cada componente en [`deploy/observability/README.md`](../deploy/observability/README.md) y cómo consultar los datos en [`Observability.md`](Observability.md).

### 4.3 Si un puerto está ocupado

Los puertos están fijados en los archivos compose. Para cambiar uno, edita el mapeo del servicio afectado — **el primer número es el del host**, el segundo el del contenedor, y solo se toca el primero:

```yaml
ports:
  - "1435:1433"      # SQL Server ahora en localhost:1435
```

Archivos: `deploy/database/docker-compose.sqlserver.yml` y `deploy/observability/docker-compose.observability.yml`. Si cambias el de SQL Server, actualiza también el connection string; si cambias el del collector, actualiza `OpenTelemetry:OtlpEndpoint`.

Para ver qué proceso ocupa un puerto en Windows:

```pwsh
Get-NetTCPConnection -LocalPort 1433 -State Listen | Select-Object OwningProcess
```

---

## 5. Paso 3 — Migraciones

Con SQL Server accesible y el connection string configurado:

```pwsh
dotnet ef database update `
  --project Company.Product.Modules.Identity.Infrastructure `
  --startup-project Company.Product.WebApi
```

Crea la base de datos, el schema `identity` y su tabla de historial `identity.__EFMigrationsHistory`, y aplica las 4 migraciones en orden:

1. `20260304152336_InitialIdentity`
2. `20260507202105_AddUserProfile`
3. `20260507203931_AddUserLockout`
4. `20260507203947_AddRefreshTokens`

Para comprobar el estado en cualquier momento:

```pwsh
dotnet ef migrations list `
  --project Company.Product.Modules.Identity.Infrastructure `
  --startup-project Company.Product.WebApi
```

---

## 6. Paso 4 — Ejecutar

```pwsh
dotnet run --project Company.Product.WebApi
```

Perfiles disponibles (`--launch-profile <nombre>`):

| Perfil | URLs | Cuándo usarlo |
|---|---|---|
| `http` (por defecto) | `http://localhost:5070` | Día a día, sin certificados |
| `https` | `https://localhost:7297` + `http://localhost:5070` | Cuando necesitas HTTPS real |
| `Local+OTel` | igual que `https` | Con el stack de observabilidad levantado; además exporta spans a consola |

Al arrancar en `Development` el seed crea, de forma idempotente: los 6 permisos, el rol `Admin` con todos ellos, y el usuario `admin@company.com` / `Admin123!`.

- Swagger → <http://localhost:5070/swagger>
- Info del servicio → <http://localhost:5070/_info>
- Health → <http://localhost:5070/health/live> y <http://localhost:5070/health/ready>

---

## 7. Paso 5 — Validación funcional

```pwsh
# 1. Login con el admin del seed
$login = Invoke-RestMethod -Uri http://localhost:5070/api/auth/login `
  -Method Post -ContentType "application/json" `
  -Body '{"usernameOrEmail":"admin@company.com","password":"Admin123!"}'
$token = $login.accessToken

# 2. Perfil del usuario autenticado
Invoke-RestMethod -Uri http://localhost:5070/api/users/me `
  -Headers @{ Authorization = "Bearer $token" }

# 3. Permisos efectivos
Invoke-RestMethod -Uri http://localhost:5070/api/users/me/permissions `
  -Headers @{ Authorization = "Bearer $token" }
```

Resultado esperado:

| Paso | Respuesta |
|---|---|
| 1 | `accessToken` (JWT) + cookie `refresh_token` |
| 2 | Datos del admin: email, userName, perfil |
| 3 | Los 6 permisos: `users.read/write`, `roles.read/write`, `permissions.read/write` |

También puedes usar `Company.Product.WebApi/Company.Product.WebApi.http` (Visual Studio, o VS Code con la extensión REST Client): pega el `accessToken` en la variable `@token` del inicio y ejecuta cualquier bloque.

---

## 8. Paso 6 — Tests

```pwsh
# Todo de una vez (necesita Docker por la suite RealDb)
dotnet test

# Suite rápida: unit + arquitectura + integración con SQLite. No necesita Docker.
dotnet test -- --filter-not-trait "Category=RealDb" --ignore-exit-code 8

# Solo la suite contra SQL Server real (Testcontainers). Necesita Docker corriendo.
dotnet test -- --filter-trait "Category=RealDb" --ignore-exit-code 8

# Con cobertura
dotnet test -- --coverage --coverage-output-format cobertura
```

> **Sobre la sintaxis:** xunit.v3 corre sobre Microsoft Testing Platform (el runner está
> declarado en `global.json`). Lo que va después de `--` son argumentos de la app de test, no
> del SDK. `--ignore-exit-code 8` es necesario porque, al filtrar, algún proyecto se queda sin
> tests que ejecutar y el runner lo trataría como fallo. Sin filtro (`dotnet test` a secas) corre
> todo y no hace falta.

Referencia de lo que debes ver: **206 tests verdes** en total — 200 en la suite rápida (121 unit + 20 arquitectura + 59 integración) y 6 en la RealDb.

La suite RealDb levanta su propio contenedor de SQL Server en un puerto aleatorio, así que **no** choca con el de `dev/database.ps1`.

---

## 9. La aplicación completa en contenedores (opcional)

Para una demo, para QA o para validar la imagen antes de desplegar, hay un compose que levanta base de datos + migraciones + API:

```pwsh
docker compose -f deploy/app/docker-compose.yml up --build
```

Arranca en orden: SQL Server → `migrator` (aplica migraciones y termina) → API en <http://localhost:8080>. Usa puertos distintos a los del entorno local (8080 y 1434), así que puedes tenerlo corriendo a la vez que tu `dotnet run`.

```pwsh
docker compose -f deploy/app/docker-compose.yml down      # parar
docker compose -f deploy/app/docker-compose.yml down -v   # parar y borrar datos
```

Para construir solo la imagen:

```pwsh
docker build -t company-product-api .
```

> La imagen final corre como usuario sin privilegios, expone el puerto 8080 y trae `HEALTHCHECK` contra `/health/live`. Los argumentos de build `COMMIT_SHA` y `BUILD_DATE` alimentan el endpoint `/_info`.

---

## 10. Configurar el IDE

**Visual Studio 2022/2026** — abre `Company.Product.slnx`. El proyecto de arranque es `Company.Product.WebApi`; el selector de perfiles (`http`, `https`, `Local+OTel`) está junto al botón de ejecutar.

**VS Code** — instala *C# Dev Kit* y *REST Client* (para el `.http`).

**Rider** — abre el `.slnx` directamente.

> **Mapperly:** los mappers son código generado en compilación. Hasta el primer `dotnet build`, el IntelliSense marca en rojo métodos que sí existen. Compila antes de sacar conclusiones.

---

## 11. Troubleshooting

| Síntoma | Causa | Solución |
|---|---|---|
| `Could not execute because the specified command or file was not found: dotnet ef` | Falta restaurar las herramientas locales | `dotnet tool restore` |
| `MSB3552: Resource file "**/*.resx" cannot be found` al correr `dotnet ef` | Versión de `dotnet-ef` más vieja que el SDK | Actualiza `.config/dotnet-tools.json` y `dotnet tool restore` |
| `A network-related or instance-specific error ... SQL Server` | El contenedor no está arriba o el connection string apunta a otro sitio | `.\dev\database.ps1 up` y revisa `ConnectionStrings` |
| `Login failed for user 'sa'` | Contraseña distinta a la del compose | La del contenedor es `Password123!` |
| El contenedor de SQL Server se queda en `unhealthy` | Poca memoria asignada a Docker | Sube la RAM de Docker Desktop a ≥ 4 GB; revisa `.\dev\database.ps1 logs` |
| `Bind for 0.0.0.0:1433 failed: port is already allocated` | Ya hay un SQL Server escuchando | Párale, o cambia el puerto del host (§ 4.3) |
| La app no arranca: error de validación de `JwtOptions` | `Jwt:Key` ausente o < 32 caracteres | Ajusta `appsettings.Development.json` |
| `Missing connection string: DefaultConnection` / `Db` | Falta uno de los dos | Ambos deben tener valor; `Db` lo usa el health check |
| Swagger no abre en `https://localhost:7297` | Falta el certificado de desarrollo | `dotnet dev-certs https --trust` |
| Errores rojos en mappers recién creados | Mapperly aún no ha generado el código | `dotnet build` |
| Los tests `RealDb` fallan al arrancar el contenedor | Docker apagado | Arranca Docker Desktop y reintenta |
| `Testing with VSTest target is no longer supported by Microsoft.Testing.Platform` | Falta el `global.json` que declara el runner, o ejecutas `dotnet test` fuera del repositorio | Ejecuta desde la raíz del repo; `global.json` debe declarar `"test": { "runner": "Microsoft.Testing.Platform" }` |
| `dotnet test` con filtro termina en código 8 aunque todo esté verde | Un proyecto se quedó sin tests tras filtrar y el runner lo trata como fallo | Añade `--ignore-exit-code 8` (ver § 8) |
| `warning xUnit1051` al escribir un test nuevo | Falta pasar el token de cancelación del test | Usa `TestContext.Current.CancellationToken` en toda llamada que acepte `CancellationToken` |
| `warning NU1903: ... vulnerabilidad conocida` | Un paquete quedó desactualizado | `dotnet list package --vulnerable --include-transitive` y sube el paquete padre en `Directory.Packages.props` |
| No llegan trazas a Grafana | OTel apagado o endpoint equivocado | `OpenTelemetry:Enabled: true` y `OtlpEndpoint: http://localhost:4317`, con el stack levantado |

---

## 12. Checklist de "listo para trabajar"

- [ ] `dotnet --version` imprime 10.x
- [ ] `docker compose version` responde (v2)
- [ ] `dotnet tool restore` y `dotnet ef --version` funcionan
- [ ] Existe `appsettings.Development.json` con connection string y `Jwt:Key` propios
- [ ] SQL Server accesible (contenedor, LocalDB o instancia de red)
- [ ] `dotnet ef database update` aplicó las 4 migraciones
- [ ] `dotnet run --project Company.Product.WebApi` arranca sin errores
- [ ] Swagger abre y el login del admin devuelve `accessToken`
- [ ] `GET /api/users/me/permissions` devuelve los 6 permisos
- [ ] `dotnet test -- --filter-not-trait "Category=RealDb" --ignore-exit-code 8` → 200 verdes
- [ ] (Opcional) `dotnet test -- --filter-trait "Category=RealDb" --ignore-exit-code 8` → 6 verdes con Docker arriba

---

## 13. Siguientes pasos

- **Adaptar la plantilla a tu proyecto** → [`04-Guia-Renombrado-y-Repositorio.md`](04-Guia-Renombrado-y-Repositorio.md)
- **Entender la arquitectura** → [`01-Arquitectura.md`](01-Arquitectura.md)
- **Agregar un módulo, entidad, comando o endpoint** → [`Standards.md`](Standards.md)
- **Trabajar con asistentes de IA sobre este repo** → [`02-Instrucciones-IA.md`](02-Instrucciones-IA.md)
