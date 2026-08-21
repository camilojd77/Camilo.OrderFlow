# Documento 4 — Renombrar la plantilla y apuntar a un repositorio nuevo

> **Audiencia:** quien arranca un proyecto nuevo a partir de esta plantilla.
> **Objetivo:** que `Company.Product` desaparezca por completo del proyecto derivado y que el repositorio apunte a donde debe.
> **Tiempo estimado:** 20–40 minutos, incluida la verificación.

**Antes de empezar**, deja la plantilla corriendo tal cual siguiendo [`03-Guia-Nuevo-Equipo.md`](03-Guia-Nuevo-Equipo.md). Renombrar sobre algo que nunca arrancó mezcla dos fuentes de error.

---

## 1. Elegir el nombre

La convención es `<Empresa>.<Producto>`, en PascalCase, sin espacios ni acentos: `Acme.Inventory`, `Emast.Facturacion`.

De ese nombre salen tres formatos, y **los tres** aparecen en el repositorio:

| Formato | Plantilla | Ejemplo | Dónde vive |
|---|---|---|---|
| PascalCase con punto | `Company.Product` | `Acme.Inventory` | Namespaces, nombres de proyecto, `using`, referencias, carpetas |
| kebab-case | `company-product` | `acme-inventory` | Contenedores, proyectos de compose, labels de Loki, datasources de Grafana |
| Sin puntos | `CompanyProduct` | `AcmeInventory` | Nombre de la base de datos (`CompanyProductDev`) |

Decide también:

- **Nombre de la base de datos** — por defecto `<SinPuntos>Dev` en desarrollo.
- **Dominio del admin semilla** — hoy `admin@company.com`.
- **Schema del módulo** — hoy `identity`. Solo cámbialo si tu organización lo exige; no aporta nada renombrarlo.

---

## 2. Inventario: qué se toca al renombrar

Medido sobre el repositorio real:

| Token | Alcance |
|---|---|
| `Company.Product` | **~230 archivos**: todos los `.cs`, los 9 `.csproj`, el `.slnx`, migraciones, tests, docs, `Dockerfile`, `.http` |
| `company-product` | `deploy/database/docker-compose.sqlserver.yml` (`container_name`), `deploy/observability/docker-compose.observability.yml` (`name:` del proyecto), `deploy/app/docker-compose.yml`, `grafana/provisioning/datasources/datasources.yml`, `dev/database.ps1`, `appsettings.Development.json.example` (label de Loki), `README.md` |
| `CompanyProduct` | `appsettings.Development.json.example` (nombre de BBDD) |

Además hay **13 archivos** y **10 carpetas** cuyo *nombre* contiene el token.

Fuera de los tres tokens, quedan estos puntos que ningún reemplazo automático puede decidir por ti:

| Punto | Dónde | Por qué importa |
|---|---|---|
| Nombre de la carpeta raíz | Sistema de archivos | Cosmético, pero confunde si no coincide |
| Email del admin semilla | `IdentityDataSeeder.cs` (`admin@company.com`) | Es una credencial del producto, no un identificador técnico |
| Puertos del host | Los tres compose y `launchSettings.json` | Dos proyectos derivados en la misma máquina chocan en 1433, 3000, 5070… |
| `Jwt:Issuer` / `Jwt:Audience` | `appsettings*.json` | Se renombran con el token, pero confirma que es lo que quieres emitir |
| `OpenTelemetry:ServiceName` | `appsettings*.json` | Es la etiqueta con la que buscarás en Grafana |
| Migraciones EF | `Modules.Identity.Infrastructure/Migrations` | Ver § 4 |

---

## 3. Renombrado

### 3.1 Con el script (recomendado)

```pwsh
# 1. Cierra el IDE y limpia salidas de compilación
Get-ChildItem -Recurse -Directory -Include bin,obj | Remove-Item -Recurse -Force

# 2. Simula: no escribe nada, solo te dice qué haría
.\dev\rename.ps1 -NewName "Acme.Inventory" -WhatIf

# 3. Aplica
.\dev\rename.ps1 -NewName "Acme.Inventory"
```

El script sustituye los tres formatos del token, renombra archivos y carpetas (de la más profunda a la más superficial) y respeta el BOM de cada archivo. **No** toca `.git`, ni `bin/`, `obj/`, `.vs/`, `.vscode/`, `.claude/`, ni a sí mismo.

Al terminar imprime lo que queda pendiente a mano. Añade `-Verbose` si quieres ver el detalle archivo por archivo.

### 3.2 A mano

Si prefieres no ejecutar el script, este es el orden que evita romper el build a mitad:

1. Cierra el IDE y borra `bin/`, `obj/`, `.vs/`.
2. Reemplazo global de contenido en archivos de texto, en este orden:
   `Company.Product` → `Acme.Inventory`, `company-product` → `acme-inventory`, `CompanyProduct` → `AcmeInventory`.
3. Renombra los archivos cuyo nombre contiene el token (9 `.csproj`, el `.slnx`, el `.http`, los `.Designer.cs` no hace falta).
4. Renombra las carpetas de proyecto, de la más profunda a la más superficial.
5. Comprueba que las rutas del `.slnx` apuntan a los `.csproj` renombrados.

### 3.3 Después del renombrado, a mano en ambos casos

1. **Carpeta raíz** del repositorio → `Acme.Inventory`.
2. **Admin semilla** en `Company.Product.Modules.Identity.Infrastructure/Persistence/Seed/IdentityDataSeeder.cs`: cambia `admin@company.com` por el dominio real. Aparece dos veces (creación y búsqueda).
3. **Base de datos**: ajusta `Database=...` en tu `appsettings.Development.json`.
4. **Puertos**, solo si vas a tener varios proyectos derivados en la misma máquina:

   | Servicio | Archivo | Puerto por defecto |
   |---|---|---|
   | API local | `Company.Product.WebApi/Properties/launchSettings.json` | 5070 / 7297 |
   | SQL Server dev | `deploy/database/docker-compose.sqlserver.yml` | 1433 |
   | API en contenedor | `deploy/app/docker-compose.yml` | 8080 / 1434 |
   | Observabilidad | `deploy/observability/docker-compose.observability.yml` | 3000, 3100, 3200, 4317, 4318, 8888, 8889, 9090 |

---

## 4. Decisión: conservar o regenerar las migraciones

**Recomendado: conservarlas.** Las 4 migraciones actuales están validadas y aplican limpiamente sobre una BBDD vacía. Renombrar cambia sus namespaces, no su contenido SQL, así que siguen siendo válidas.

Regenerarlas desde cero solo tiene sentido si vas a cambiar el modelo de Identity antes del primer deploy. En ese caso:

```pwsh
Remove-Item -Recurse Acme.Inventory.Modules.Identity.Infrastructure\Migrations
dotnet ef migrations add InitialIdentity `
  --project Acme.Inventory.Modules.Identity.Infrastructure `
  --startup-project Acme.Inventory.WebApi
```

> Solo si **nadie** ha desplegado todavía. Una migración ya aplicada en cualquier entorno no se regenera: se añade una nueva encima.

---

## 5. Apuntar a un repositorio nuevo

Primero crea el repositorio vacío en tu organización (sin README, sin .gitignore: ya los tienes). Luego elige un camino.

### Opción A — Historia limpia (recomendada para un producto nuevo)

El proyecto derivado arranca con un único commit. La historia de la plantilla no aporta a un equipo que nunca la construyó, y evita arrastrar referencias a decisiones de otro producto.

```pwsh
Remove-Item -Recurse -Force .git
git init -b main
git add .
git commit -m "Base inicial a partir de la plantilla Company.Product"
git remote add origin <url-del-repo-nuevo>
git push -u origin main
```

### Opción B — Conservar la historia

Útil si quieres poder rastrear por qué la plantilla es como es, o si vas a seguir trayendo mejoras de la plantilla original.

```pwsh
git remote -v                                  # ver a dónde apunta ahora
git remote set-url origin <url-del-repo-nuevo>
git push -u origin main
```

Si además quieres seguir recibiendo cambios de la plantilla, deja la original como segundo remoto:

```pwsh
git remote add template <url-de-la-plantilla>
git fetch template
# más adelante: git merge template/main   (esperando conflictos por el renombrado)
```

> Traer cambios de la plantilla después de renombrar genera conflictos en casi todos los archivos, porque cambiaron los namespaces. Es viable, pero no lo asumas gratis.

### 5.1 Configuración del repositorio

- Rama por defecto `main`. Si tu equipo usa `develop`, créala: el CI ya dispara sobre `main` y `develop`.
- Protege `main`: exige PR y que el check de CI pase.
- Revisa `.github/workflows/ci.yml`:
  - Corre en `ubuntu-latest` con .NET 10 y compila con `/p:TreatWarningsAsErrors=true`; un aviso de paquete vulnerable **rompe el build** a propósito.
  - `dorny/test-reporter@v1` necesita permisos de checks; si tu organización restringe acciones de terceros, sustitúyela o quita ese paso.
  - Si tu organización usa **Azure DevOps** en vez de GitHub Actions, el final del mismo archivo trae el pipeline equivalente comentado: cópialo a `azure-pipelines.yml` y borra el workflow.
- Secretos: la plantilla no necesita ninguno para el CI (los tests usan SQLite y Testcontainers).

---

## 6. Verificación

```pwsh
# 1. No debe quedar ningún rastro del nombre viejo
git grep -l "Company\.Product"    # sin resultados
git grep -l "company-product"     # sin resultados
git grep -l "CompanyProduct"      # sin resultados

# 2. Compila y pasa la suite
dotnet restore
dotnet build /p:TreatWarningsAsErrors=true
dotnet test                                    # 206 verdes (200 + 6 RealDb), con Docker arriba

# 3. Arranca y prueba
dotnet run --project Acme.Inventory.WebApi
```

Checklist final:

- [ ] Los tres `git grep` no devuelven nada
- [ ] `dotnet build /p:TreatWarningsAsErrors=true` sin warnings ni errores
- [ ] `dotnet test` en verde (206: 200 de la suite rápida + 6 de RealDb)
- [ ] La API arranca y Swagger abre
- [ ] Login del admin devuelve `accessToken` y `GET /api/users/me/permissions` devuelve los 6 permisos
- [ ] `.\dev\database.ps1 up` levanta el contenedor con el nombre nuevo
- [ ] `docker compose -f deploy/app/docker-compose.yml up --build` arranca la app completa
- [ ] En Grafana, las trazas aparecen bajo el nuevo `ServiceName`
- [ ] `git remote -v` apunta al repositorio nuevo
- [ ] El primer push disparó el CI y quedó verde

---

## 7. Qué NO renombrar

| Elemento | Motivo |
|---|---|
| `__EFMigrationsHistory` | Nombre estándar de EF Core; cambiarlo no aporta y rompe herramientas |
| Ids de paquetes NuGet | `Microsoft.*`, `Serilog.*`, etc. son externos |
| La clase `Program` | `WebApplicationFactory<Program>` la busca por nombre desde los tests |
| Los timestamps de las migraciones | Forman parte de su identidad; renombrarlos rompe el historial |
| `identity` como schema | Es el nombre del módulo, no del producto |
| Los códigos de permiso (`users.read`, …) | Son contrato de la API; cambiarlos obliga a re-sembrar y a tocar los clientes |
