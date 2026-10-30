# Known limitations

Deliberate shortcuts and open items of the current version. Planned larger steps are listed in the
[Roadmap](../README.md#roadmap).

## Security

- **Data Protection keys are not persisted.** They live in the container file system, separately per
  replica. This is harmless today (the JWT uses its own symmetric key and the API is stateless), but
  they must move to Blob Storage + Key Vault (`PersistKeysToAzureBlobStorage`,
  `ProtectKeysWithAzureKeyVault`) before cookies, antiforgery tokens or `IDataProtector` are used.
- **No refresh tokens and no token revocation.** An access token is valid for 60 minutes
  (`JwtSettings:AccessTokenExpirationMinutes`), after which the user has to sign in again. Signing out
  only discards the token in the browser; the token itself stays valid until it expires.
- **No rate limiting**, not even on the login and registration endpoints.
- **Docker Compose connects as `sa`.** Azure already uses least-privilege identities; locally a
  dedicated app login (`db_datareader` / `db_datawriter`) and a migrator login (`db_ddladmin`) are
  still to do.

## Operations

- **Health responses are plain text**, and the database check has no explicit timeout, so a hanging
  SQL Server can block `/health/ready`. See [Health endpoints](observability.md#health-endpoints).
- **`OpenTelemetry.Instrumentation.EntityFrameworkCore` is a beta package**; it should be updated
  once a stable version is released.
- **The OpenAPI drift check is disabled** in CI until it is stabilised, so a change of the API
  contract without regenerating the TypeScript client is not caught automatically. See
  [CI jobs](ci-cd.md#ci-jobs).

## Features

- **Payment is simulated.** Paying an order marks it as paid and issues the tickets; there is no
  payment provider behind it.
- **No admin order management in the SPA yet** (order list, manual status change, refunds).
- **No location management screen in the SPA.** Locations can be managed through the API only.
