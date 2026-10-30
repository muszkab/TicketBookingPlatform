# Ticket Booking Platform

A demo event ticketing platform built as a portfolio project to showcase **.NET backend
engineering with Clean Architecture**, exposing a versioned **REST API** consumed by an
**Angular SPA** through a generated OpenAPI client.

---

## Azure deployment

The API runs on Azure Container Apps against Azure SQL, with every secret in Key Vault. **No password
exists anywhere in the system** — neither in the images, nor in the app configuration, nor in the
vault: the connection string itself uses a Managed Identity.

| Resource | Role |
|---|---|
| Container App (`api`) | Runs the published API image, HTTP ingress on **port 8080**. |
| Container Apps Job (`migrator`) | Manually triggered, one-shot EF Core migration + seeding. `parallelism = 1`, so two migrations can never run at the same time (replica timeout 600 s, 1 retry). |
| Azure SQL Database | Application database, reached with Entra authentication. |
| Key Vault | Connection string, JWT signing key and seed admin password. |
| Static Web App | Hosts the SPA (Free tier) and calls the API directly, with CORS. |
| Log Analytics workspace | Console and system logs of the environment. |

Both the app and the job have their own **system-assigned Managed Identity**, each granted the
**Key Vault Secrets User** role. The database permissions are deliberately asymmetric:

```sql
-- API: reads and writes data, but cannot change the schema
CREATE USER [<container-app-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [<container-app-name>];
ALTER ROLE db_datawriter ADD MEMBER [<container-app-name>];

-- Migrator job: the only identity allowed to change the schema
CREATE USER [<job-name>] FROM EXTERNAL PROVIDER;
ALTER ROLE db_ddladmin   ADD MEMBER [<job-name>];
ALTER ROLE db_datareader ADD MEMBER [<job-name>];
ALTER ROLE db_datawriter ADD MEMBER [<job-name>];
```

The passwordless connection string stored in the vault:

```
Server=tcp:<server>.database.windows.net,1433;Initial Catalog=TicketBookingPlatform;Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;
```

Container App environment variables (the secrets come from the vault, not from here):
`KeyVault__Uri`, `ASPNETCORE_ENVIRONMENT=Production`, `ForwardedHeaders__Enabled=true`,
`Https__RedirectEnabled=false`, `Https__HstsEnabled=false` — TLS terminates at the ingress.

Releases are deployed automatically from a git tag, see
[Releasing a new version](ci-cd.md#releasing-a-new-version).

### Deploying the API

`.github/workflows/deploy-api.yml` signs in to Azure with **OIDC** (a federated credential), so no
client secret is stored in GitHub. The job runs in the `production` GitHub Environment, which is also
what the federated credential trusts. Add *required reviewers* to that environment if you ever want a
manual approval step before a deploy.

One-time Azure setup:

```bash
appId=$(az ad app create --display-name github-ticketbooking-deploy --query appId -o tsv)
az ad sp create --id $appId

az ad app federated-credential create --id $appId --parameters '{
  "name": "github-production",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<owner>@<owner-id>/<repo>@<repo-id>:environment:production",
  "audiences": ["api://AzureADTokenExchange"]
}'

az role assignment create --assignee $appId --role Contributor \
  --scope /subscriptions/<subscription-id>/resourceGroups/rg-ticketbooking
```

The subject must match what GitHub actually presents. For this repository GitHub sends the **ID-based**
form (`repo:<owner>@<owner-id>/<repo>@<repo-id>:environment:production`, the numeric ids come from
`https://api.github.com/repos/<owner>/<repo>`), not the plain `repo:<owner>/<repo>:...` one. If the login
fails with `AADSTS700213`, copy the *subject claim* printed by the `azure/login` step into the credential.

Prerequisites in the GitHub repository (create the `production` environment first; all three are
**environment secrets** of `production`, like the Static Web Apps token below):

| Kind | Name | Value |
|---|---|---|
| Environment secret | `AZURE_CLIENT_ID` | The `appId` of the app registration above. |
| Environment secret | `AZURE_TENANT_ID` | The Entra tenant id. |
| Environment secret | `AZURE_SUBSCRIPTION_ID` | The subscription that holds `rg-ticketbooking`. |

The resource names (`rg-ticketbooking`, `containerapp-ticketbooking-api`,
`job-ticketbooking-migrator`) are set in the workflow's `env` block. The images are public on GHCR,
so the Container App needs no registry credentials.

### Probes

Map the health endpoints to the container probes (`/health/live` for liveness and startup,
`/health/ready` for readiness, port `8080`). Give the startup probe a generous window (an initial
delay of about 10 s and a failure threshold of about 30 work well): the app needs a few seconds to
read the vault and warm up, and the first `/health/ready` call also pays for acquiring the Managed
Identity token. "Probe of StartUp failed" entries before that are just noise.

### SPA and CORS

The SPA is hosted on Azure Static Web Apps and calls the Container App **directly**, so the two run
on different origins and CORS is required. Two settings have to match:

1. The SPA's `config.json` — `apiBasePath` set to the Container App URL.
2. The API — `Cors__AllowedOrigins__0` set to the Static Web Apps URL.

```
Cors__AllowedOrigins__0 = https://<spa-name>.azurestaticapps.net
```

The CORS middleware is only registered when the list is non-empty, which is why local development
and Docker Compose (where nginx proxies `/api/` on the same origin) need no CORS configuration at
all. Every additional origin — a custom domain or a staging slot — needs its own indexed entry
(`Cors__AllowedOrigins__1`, ...); the policy uses `AllowCredentials`, so wildcards are not allowed.

> The alternative would be the Static Web Apps *linked backend* feature, which proxies the API under
> the SPA origin and removes the need for CORS entirely. It requires the Standard plan, so the
> direct-call approach above was chosen instead.

### Deploying the SPA

`.github/workflows/deploy-spa.yml` builds the Angular application and uploads it to Static Web Apps.
On a `v*.*.*` tag it is called from `ci.yml` once the API deploy has succeeded; it can also be run on
demand via *Run workflow*. It writes `config.json` into the build
output from the `API_BASE_PATH` variable, so the same source produces the right API URL
per environment. A manual run can override it with the `api_base_path` input; if neither is set, the
workflow fails instead of shipping an SPA without an API URL.

Prerequisites in the GitHub repository:

| Kind | Name | Value |
|---|---|---|
| Environment secret (`production`) | `AZURE_STATIC_WEB_APPS_API_TOKEN` | The deployment token from the Static Web App (*Overview → Manage deployment token*). |
| Environment variable (`production`) | `API_BASE_PATH` | The Container App URL, e.g. `https://<app>.<region>.azurecontainerapps.io`. |

`public/staticwebapp.config.json` handles the routing: unknown paths fall back to `index.html` (the
Angular router owns them) and `config.json` is served with `Cache-Control: no-store`.

When creating the Static Web App in the portal, choose **Deployment source: Other** — the workflow
above already does the build and upload, so Azure should not generate its own.

---

## Known limitations

- **Data Protection keys are not persisted.** They live in the container file system, separately per
  replica. This is harmless today (the JWT uses its own symmetric key and the API is stateless), but
  they must move to Blob Storage + Key Vault (`PersistKeysToAzureBlobStorage`,
  `ProtectKeysWithAzureKeyVault`) before cookies, antiforgery tokens or `IDataProtector` are used.
- **Docker Compose connects as `sa`.** Azure already uses least-privilege identities; locally a
  dedicated app login (`db_datareader` / `db_datawriter`) and a migrator login (`db_ddladmin`) are
  still to do.
- **Health responses are plain text**, and the database check has no explicit timeout.
- **`OpenTelemetry.Instrumentation.EntityFrameworkCore` is a beta package**; it should be updated
  once a stable version is released.
- **No admin order management in the SPA yet** (order list, manual status change, refunds).

## Roadmap

- [ ] Upgrade to .NET 10 (LTS)
- [x] Docker Compose (SQL Server + API + SPA)
- [ ] FluentValidation for command/query input validation
- [ ] Testcontainers (real SQL Server) for the integration tests
- [x] CI/CD pipeline (build, test, OpenAPI drift check, container image publish)
- [x] Cloud-native readiness (health checks, OpenTelemetry, externalised configuration)
- [x] Deployment to Azure (Container Apps + Azure SQL + Static Web Apps, secrets in Key Vault)
