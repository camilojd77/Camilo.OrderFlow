# ADR-0010 — Identidad propia con BCrypt y JWT sin tolerancia de reloj

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** la organización adopte un proveedor de identidad (Entra ID, Keycloak…) o se exija otro algoritmo
  de hash (p. ej. Argon2).

## Contexto

La plantilla necesitaba usuarios, roles, permisos, bloqueo por intentos y refresh tokens sin depender de un proveedor
externo. Alternativas: ASP.NET Core Identity (trae su propio esquema y modelo de roles), un IdP externo con OIDC
(infraestructura adicional), o hashing con PBKDF2 del framework.

## Decisión

- Módulo `Identity` propio; **no** se usa `Microsoft.AspNetCore.Identity`.
- Contraseñas con **BCrypt** (`BCrypt.Net-Next`) detrás de `IPasswordHasher`.
- **JWT Bearer** firmado con clave simétrica: `Jwt:Key` ≥ 32 caracteres (validado al arrancar), se validan emisor,
  audiencia, firma y vida con `ClockSkew = TimeSpan.Zero`. Refresh tokens persistidos como hash, con rotación.
- Bloqueo tras `Lockout:MaxAttempts` intentos durante `Lockout:DurationMinutes`.

## Consecuencias

- Control total del modelo y sin dependencias de terceros; a cambio, la seguridad del módulo es responsabilidad del
  equipo.
- Deuda abierta (informe de fase cero): revocación de access tokens al desactivar un usuario y consumo atómico de
  refresh tokens concurrentes.
- No se debilitan `ClockSkew`, la longitud de clave ni el algoritmo sin un ADR nuevo.

## Referencias

`Camilo.OrderFlow.Modules.Identity.Infrastructure/Auth/*`, `Camilo.OrderFlow.WebApi/Program.cs` (JWT Bearer),
`Camilo.OrderFlow.WebApi/Controllers/AuthController.cs`, `appsettings.json` (`Jwt`, `Lockout`, `RefreshToken`).
