# ADR-0029 — Identidad propia con revocación inmediata de sesiones y rotación segura de refresh tokens

- **Estado:** Aceptada
- **Fecha:** 2026-11-05 · **Decide:** Camilo Jaramillo
- **Revisar cuando:** la organización adopte un proveedor de identidad (Entra ID, Keycloak…), se exija otro algoritmo
  de hash, o la consulta por petición se note en la latencia.

## Contexto

Reemplaza a [ADR-0010](0010-identidad-propia-bcrypt-jwt.md), que dejaba dos deudas abiertas:

1. Un usuario desactivado seguía usando su access token hasta que caducaba (60 min): el JWT solo se validaba por
   firma, emisor, audiencia y vida. Reproducido con un test de integración.
2. `RefreshAsync` leía el token activo, lo revocaba y emitía otro sin control de concurrencia: dos peticiones
   simultáneas con el mismo refresh token obtenían dos tokens válidos, y un token ya rotado que reapareciera (robo)
   no tenía consecuencias.

Alternativas para (1): access tokens muy cortos (sigue habiendo ventana) o un *security stamp* en el token (necesita
columna y migración). Para (2): concurrencia optimista con `rowversion` (migración) o solo consumo atómico, sin
detección de reutilización.

## Decisión

Se mantiene lo de ADR-0010:

- Módulo `Identity` propio, sin `Microsoft.AspNetCore.Identity`.
- Contraseñas con BCrypt detrás de `IPasswordHasher`.
- JWT Bearer con clave simétrica de 32 caracteres o más y `ClockSkew = 0`.
- Refresh tokens persistidos como hash.
- Bloqueo tras `Lockout:MaxAttempts` intentos fallidos.

Y se añade:

- **Usuario activo en cada petición.** Tras validar el JWT (`OnTokenValidated`), el host comprueba con
  `IAuthService.IsActiveUserAsync` que el usuario existe y está activo; si no, el token se rechaza (401). Es una
  consulta por clave primaria, sin caché, para que el efecto sea inmediato también con varias réplicas.
- **Desactivar revoca los refresh tokens** activos del usuario en la misma transacción.
- **Consumo atómico del refresh token.** La rotación revoca el token con una actualización condicional («solo si aún
  no está revocado»). Si dos peticiones lo presentan a la vez, gana una y la otra recibe 401. No hace falta migración.
- **Detección de reutilización.** Si se presenta un refresh token que ya fue rotado, se revocan todos los refresh
  tokens activos del usuario (posible robo) y se responde 401. Es lo que recomienda OAuth 2.0 Security Best Current
  Practice (RFC 9700).

## Consecuencias

- Desactivar a un usuario corta sus sesiones al momento.
- Cada petición autenticada hace una consulta más a la base de datos; si llega a pesar, se revisa este ADR (caché con
  invalidación o *security stamp*).
- Un cliente que reintente un refresh ya usado pierde la sesión y tiene que volver a iniciar sesión.
- No se debilitan `ClockSkew`, la longitud de clave, el algoritmo ni estas comprobaciones sin un ADR nuevo.

## Referencias

`Camilo.OrderFlow.WebApi/Program.cs` (`OnTokenValidated`), `Camilo.OrderFlow.Modules.Identity.Infrastructure/Auth/*`,
`Identity.Application/Commands/Users/DeactivateUser/*`, `Persistence/Repositories/RefreshTokenRepository.cs`,
`tests/Camilo.OrderFlow.WebApi.IntegrationTests/SessionRevocationTests.cs`.
