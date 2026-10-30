# Configuration

No connection string or secret is committed to the repository. Settings come from several sources,
and a later source overrides an earlier one:

1. `appsettings.json`: non-secret defaults.
2. `appsettings.Development.json`: local development overrides (Development environment only).
3. User secrets: local secrets (Development environment only).
4. Environment variables: containers, Container Apps.
5. Command-line arguments.
6. Azure Key Vault: only when `KeyVault:Uri` is set, see [Azure Key Vault](#azure-key-vault).

Nested keys are separated with `:` in JSON and user secrets, with `__` (double underscore) in
environment variables and with `--` in Key Vault secret names. For example `JwtSettings:SigningKey`
is `JwtSettings__SigningKey` as an environment variable and `JwtSettings--SigningKey` in Key Vault.

## Settings

| Setting | Key | Used by | Default | Notes |
|---|---|---|---|---|
| Connection string | `ConnectionStrings:DefaultConnection` | API, migrator | LocalDB (Development) | **Required.** Transient SQL errors are retried (5 attempts, at most 10 s apart). |
| JWT signing key | `JwtSettings:SigningKey` | API | – | **Required.** Use at least 32 characters: tokens are signed with HS256, which needs a 256-bit key. A missing key stops the startup, a too short one only fails when the first token is issued. |
| JWT issuer / audience | `JwtSettings:Issuer`, `JwtSettings:Audience` | API | `TicketBookingPlatform`, `TicketBookingApi` | |
| JWT lifetime | `JwtSettings:AccessTokenExpirationMinutes` | API | `60` | |
| Seed administrator | `Seed:Admin:Email`, `Seed:Admin:Password` | Migrator, API (Development) | – | **Required for seeding.** The administrator is created only if no user with this e-mail exists yet. |
| Demo data | `Migrator:SeedDemoData` | Migrator | `false` | `true` also seeds locations, events and ticket categories. Docker Compose sets it; the Development API always seeds them. |
| Startup migration | `Database:MigrateOnStartup`, `Database:SeedOnStartup` | API | `false` (`true` in Development) | **Development only.** In any other environment the API refuses to start with these enabled; use the migrator instead. |
| OTLP endpoint | `OpenTelemetry:OtlpEndpoint` | API | empty | Empty = telemetry export is disabled. |
| Key Vault | `KeyVault:Uri` | API, migrator | empty | Empty = Key Vault is not used. In Azure: `https://<vault>.vault.azure.net/`. |
| Key Vault identity | `KeyVault:ManagedIdentityClientId` | API, migrator | empty | Only for a *user-assigned* Managed Identity; leave empty for system-assigned. |
| CORS origins | `Cors:AllowedOrigins` | API | empty (`https://localhost:4200` in Development) | An array, so the environment variables are indexed: `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, … Empty = the CORS middleware is not registered. |
| HTTPS redirect | `Https:RedirectEnabled` | API | `true` | Set to `false` where TLS terminates at a proxy or ingress (containers). |
| HSTS | `Https:HstsEnabled` | API | `true` (`false` in Development) | Same as above. |
| Forwarded headers | `ForwardedHeaders:Enabled` | API | `false` | Honours `X-Forwarded-For` and `X-Forwarded-Proto` from **any** proxy, so enable it only when the API is reachable through a proxy alone (nginx, Container Apps ingress). |
| OpenAPI / Scalar | `OpenApi:Enabled` | API | `false` (`true` in Development) | Exposes `/openapi/v1.json`, `/openapi/v2.json` and the Scalar UI. |
| Log level | `Logging:LogLevel:Default` | API | `Warning` (`Information` in Development) | |
| Environment | `ASPNETCORE_ENVIRONMENT` | API | `Production` | `Development` turns on user secrets, `appsettings.Development.json` and the startup migration. |

The migrator has no `appsettings.json`: it is configured through environment variables, command-line
arguments or Key Vault only.

Missing required settings stop the process at startup instead of failing later:

- The API checks the connection string and the JWT signing key.
- The migrator checks the connection string and the administrator e-mail **before** applying any
  migration, and exits with code `2` (`1` = the migration itself failed, `0` = success).
- The administrator password is checked when the administrator account is created.

## Docker Compose

`docker compose` reads its variables from a `.env` file next to `docker-compose.yml`. Copy
`.env.example` and replace the placeholders; `.env` is git-ignored.

| Variable | Used for | Default |
|---|---|---|
| `MSSQL_SA_PASSWORD` | SQL Server `sa` password, also used in the connection string of the API and the migrator | – (required) |
| `MSSQL_DATABASE` | Database name | `TicketBookingPlatform` |
| `JWT_SIGNING_KEY` | `JwtSettings:SigningKey` | – (required) |
| `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD` | `Seed:Admin:Email`, `Seed:Admin:Password` of the migrator | – (required) |
| `API_PORT`, `SPA_PORT` | Host ports of the API and the SPA | `8080`, `4200` |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `OpenTelemetry:OtlpEndpoint` | empty |
| `ASPIRE_DASHBOARD_PORT`, `ASPIRE_DASHBOARD_OTLP_PORT` | Host ports of the optional Aspire Dashboard | `18888`, `18889` |
| `APP_VERSION` | Version baked into the images at build time, see [Versioning and image tags](documentation.md#versioning-and-image-tags) | `0.0.0` for the API and the migrator, empty for the SPA |
| `IMAGE_TAG`, `IMAGE_OWNER` | Only for `docker-compose.publishedimage.yml`: which published images to run | –, `muszkab` |

Everything else is fixed in `docker-compose.yml`: the API runs as `Production` with forwarded headers
on (the SPA container's nginx proxies `/api/` to it), HTTPS redirect and HSTS off and the OpenAPI
documents enabled; the migrator seeds the demo data.

## Azure Key Vault

When `KeyVault:Uri` is set, both the API and the migrator load an additional configuration source
from Key Vault, authenticated with `DefaultAzureCredential` (the developer's `az login` session
locally, a Managed Identity in Azure — no secret is needed to read the secrets). The provider is
opt-in, so local development and Docker Compose keep using user secrets and environment variables.

Key Vault is the last configuration source, so a secret in the vault overrides the same key coming
from an environment variable. The secrets are reloaded every 30 minutes, which picks up a rotated
value without a restart. An invalid `KeyVault:Uri` stops the startup.

Key Vault secret names cannot contain `:`, so the nested keys use `--` instead:

| Configuration key | Key Vault secret name |
|---|---|
| `ConnectionStrings:DefaultConnection` | `ConnectionStrings--DefaultConnection` |
| `JwtSettings:SigningKey` | `JwtSettings--SigningKey` |
| `Seed:Admin:Password` | `Seed--Admin--Password` |

The identity needs the **Key Vault Secrets User** role on the vault.

## SPA runtime configuration

The SPA image is built once and reused across environments, so the API base path is **not** baked
into the bundle. It is served as `config.json` next to `index.html` and fetched before Angular
bootstraps:

```json
{ "apiBasePath": "" }
```

| Value | When |
|---|---|
| `""` (empty) | Behind a same-origin proxy — the nginx container proxies `/api/` to the API, so relative requests just work and **no CORS is involved**. This is what Docker Compose uses. |
| `"https://<api-host>"` | SPA and API on different domains (Static Web Apps + Container Apps). The API must then allow that origin through `Cors:AllowedOrigins`. |

In Azure the SPA calls the API directly, see [SPA and CORS](documentation.md#spa-and-cors).

If `config.json` is missing or malformed the compile-time default from `src/environments` is kept,
so a broken deployment artifact cannot prevent the application from starting. Both nginx and Static
Web Apps serve the file with `Cache-Control: no-store`, otherwise a redeployed environment could keep
using a stale API URL.

The development server (`npm start`) ignores `config.json` and always uses the API URL from
`src/environments/environment.development.ts` (`https://localhost:5001`), because there is no proxy in
front of it.
