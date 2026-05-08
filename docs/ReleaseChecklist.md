# Release Checklist

Revisar antes de cada deploy a Staging o Production.

---

## Configuración

- [ ] `ConnectionStrings:DefaultConnection` apunta a la BBDD correcta del entorno
- [ ] `Jwt:Key` es un secreto fuerte (≥ 32 chars), distinto por entorno, **nunca en appsettings.json**
- [ ] `Jwt:Issuer` y `Jwt:Audience` coinciden con lo que emiten los tokens del entorno
- [ ] `OpenTelemetry:Enabled` = `true` y `OtlpEndpoint` apunta al collector del entorno
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

## Identidad y caché

- [ ] Existe al menos un usuario con rol **Admin** activo y contraseña fuerte
- [ ] Los permisos semilla (`users.read/write`, `roles.read/write`, `permissions.read/write`) están en la BBDD
- [ ] Si hay más de una réplica del servicio, `Cache:Redis` apunta a una instancia Redis compartida (sin Redis, `InvalidateRoleAsync` solo invalida la L1 local de la réplica que recibe la petición)
- [ ] `Lockout:MaxAttempts` y `Lockout:DurationMinutes` ajustados a la política de seguridad del cliente
- [ ] `Permissions:CacheTtlMinutes` revisado (default 10 min); reducir si los cambios de permisos deben propagarse más rápido

## Tests

- [ ] `dotnet test --filter "Category!=RealDb"` pasa en verde (unit + arch + integración SQLite)
- [ ] `dotnet test --filter "Category=RealDb"` pasa en el entorno de staging (requiere Docker o SQL Server accesible)

## Post-deploy

- [ ] `GET /_info` responde con el `commitSha` y `buildDate` del artefacto desplegado
- [ ] `GET /health/ready` verde en el nuevo entorno
- [ ] Primeras trazas/métricas visibles en Grafana dentro de los 2 primeros minutos
