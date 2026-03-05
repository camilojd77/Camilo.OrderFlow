# Company.Product — Backend Template

> Plantilla enterprise para APIs REST en .NET 10 con Clean Architecture.

## Prerrequisitos

| Herramienta | Versión mínima |
|---|---|
| .NET SDK | 10.0 |
| Docker Desktop | 4.x |
| SQL Server / LocalDB | 2019+ |

---

## Ejecutar en local

### 1. Levantar el stack de observabilidad (opcional)

```bash
docker compose -f docker/observability/docker-compose.yml up -d
```

Esto arranca **OTel Collector → Tempo → Loki → Prometheus → Grafana**.  
Grafana queda en `http://localhost:3000` (admin / admin).

### 2. Configurar secretos locales

```bash
cd Company.Product.WebApi
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=.;Database=CompanyProduct;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "your-super-secret-key-min-32-chars!!"
```

### 3. Aplicar migraciones

```bash
dotnet ef database update --project Company.Product.Modules.Identity.Infrastructure --startup-project Company.Product.WebApi
```

### 4. Ejecutar la API

```bash
dotnet run --project Company.Product.WebApi
```

Swagger disponible en `https://localhost:{port}/swagger`.  
Info de servicio en `https://localhost:{port}/_info`.

---

## Ejecutar los tests

```bash
# Todos
dotnet test

# Solo architecture tests
dotnet test tests/Company.Product.ArchitectureTests

# Con coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## Variables de entorno útiles en CI/deploy

| Variable | Descripción | Ejemplo |
|---|---|---|
| `COMMIT_SHA` | SHA del commit que generó el artefacto | `a1b2c3d` |
| `BUILD_DATE` | Timestamp ISO-8601 del build | `2025-01-15T10:30:00Z` |
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución | `Production` |

Estas aparecen en la respuesta de `GET /_info`.
