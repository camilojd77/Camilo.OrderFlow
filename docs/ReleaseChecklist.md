# Release Checklist

Revisar antes de cada deploy a Staging o Production.

---

## Configuración

- [ ] `ConnectionStrings:DefaultConnection` apunta a la BBDD correcta del entorno
- [ ] `Jwt:Key` es un secreto fuerte (≥ 32 chars), distinto por entorno, **nunca en appsettings.json**
- [ ] `Jwt:Issuer` y `Jwt:Audience` coinciden con lo que emiten los tokens del entorno
- [ ] `OpenTelemetry:Enabled` = `true` y `OtlpEndpoint` apunta al collector del entorno
- [ ] `OpenTelemetry:Environment` fijado al entorno (`staging`, `production`…): por defecto vale `dev`
- [ ] `Seed:Enabled` = `false` en Production (o confirmado si es first-deploy)
- [ ] `Cors:AllowedOrigins` contiene solo los orígenes autorizados del entorno
- [ ] `SecurityHeaders:Enabled` = `true`; revisar CSP si se sirven scripts desde CDN
- [ ] Variables de entorno `COMMIT_SHA` y `BUILD_DATE` inyectadas en el artefacto/container

## Health checks

- [ ] `GET /health/live` responde `200` antes del deploy
- [ ] `GET /health/ready` responde `200` (BBDD alcanzable, deps OK)
- [ ] Los health checks están excluidos del rate limiter y del auth

## Seguridad

- [ ] `ASPNETCORE_ENVIRONMENT` = `Production` (deshabilita Swagger, stack traces, etc.)
- [ ] HTTPS forzado (HSTS habilitado en `SecurityHeaders:HstsEnabled`)
- [ ] Secrets gestionados por Key Vault / Secret Manager, NO en repositorio
- [ ] Versiones de NuGet sin vulnerabilidades conocidas (`dotnet list package --vulnerable`)

## Performance / Resiliencia

- [ ] `RateLimiting:Default:PermitLimit` y `Auth:PermitLimit` ajustados al tráfico esperado
- [ ] Timeouts y circuit breaker de HttpClients revisados para el entorno
- [ ] `Kestrel:Limits:MaxRequestBodySizeBytes` ajustado al máximo payload esperado

## Artefacto / contenedor

- [ ] La imagen se construyó con `COMMIT_SHA` y `BUILD_DATE` (`docker build --build-arg COMMIT_SHA=$(git rev-parse --short HEAD) ...`)
- [ ] La imagen corre como usuario sin privilegios (así viene el `Dockerfile`; verificar si se personalizó)
- [ ] Las migraciones se aplican en el despliegue **antes** de arrancar la API (etapa `migrator` del `Dockerfile` o paso equivalente del pipeline)
- [ ] `HEALTHCHECK` / probes del orquestador apuntan a `/health/live` (liveness) y `/health/ready` (readiness), no al mismo

## Identidad y caché

- [ ] Existe al menos un usuario con rol **Admin** activo y contraseña fuerte
- [ ] **Primer deploy:** fuera de `Development` el seeder crea el admin con un hash placeholder inutilizable a propósito. Hay que fijarle una contraseña real antes de entregar el entorno; si no, nadie puede autenticarse
- [ ] Los permisos semilla (`users.read/write`, `roles.read/write`, `permissions.read/write`) están en la BBDD
- [ ] Si hay más de una réplica, `Cache:Redis` configurado (obligatorio). Ventana aceptada de permisos obsoletos en otras réplicas = `Permissions:LocalCacheTtlMinutes` (1 min por defecto; [ADR-0030](adr/0030-cache-de-permisos-con-ventana-acotada.md))
- [ ] `Lockout:MaxAttempts` y `Lockout:DurationMinutes` ajustados a la política de seguridad del cliente
- [ ] `Permissions:CacheTtlMinutes` revisado (default 10 min); reducir si los cambios de permisos deben propagarse más rápido

## Tests

- [ ] `dotnet test -- --filter-not-trait "Category=RealDb" --ignore-exit-code 8` pasa en verde (unit + arch + integración SQLite)
- [ ] `dotnet test -- --filter-trait "Category=RealDb" --ignore-exit-code 8` pasa en el entorno de staging (requiere Docker operativo para la fixture Testcontainers; confirmar casos realmente ejecutados)

## Post-deploy

- [ ] `GET /_info` responde con el `commitSha` y `buildDate` del artefacto desplegado
- [ ] `GET /health/ready` verde en el nuevo entorno
- [ ] Primeras trazas/métricas visibles en Grafana con demora observada y registrada
