# Azure deployment

The API runs on Azure Container Apps against Azure SQL, with every secret in Key Vault. **No password
exists anywhere in the system** — neither in the images, nor in the app configuration, nor in the
vault: the connection string itself uses a Managed Identity.

Releases are deployed automatically from a git tag, see
[Releasing a new version](ci-cd.md#releasing-a-new-version). This page describes the Azure resources
and the one-time setup the deploy workflows rely on.

```mermaid
flowchart LR
    USER[Browser] -->|static files| SWA[<b>Static Web App</b><br/>SPA]
    USER -->|API calls<br/>HTTPS, JWT, CORS| API

    subgraph ENV[Container Apps environment, Consumption plan]
        API[<b>Container App</b><br/>API]
        JOB[<b>Container Apps Job</b><br/>migrator]
    end

    API -->|Managed Identity| SQL[(<b>Azure SQL</b>)]
    JOB -->|Managed Identity| SQL
    API -->|Managed Identity| KV[<b>Key Vault</b>]
    JOB -->|Managed Identity| KV
    ENV -->|console and system logs| LA[<b>Log Analytics</b>]

    GH[<b>GitHub Actions</b>] -->|push images| GHCR[<b>GHCR</b><br/>public images]
    GHCR -->|pull| ENV
    GH -->|OIDC: update and start the job,<br/>update the app| ENV
    GH -->|deployment token:<br/>upload the SPA| SWA
```

The Static Web App only serves the SPA's static files: once loaded, the SPA runs in the browser and
calls the API directly, see [SPA and CORS](#spa-and-cors).

## Resources

| Resource | Role |
|---|---|
| Container App | Runs the published API image, HTTP ingress on **port 8080**. |
| Container Apps Job | Manually triggered, one-shot EF Core migration + seeding. `parallelism = 1`, so two migrations can never run at the same time. |
| Container Apps environment | Consumption plan, shared by the app and the job. |
| Azure SQL Database | Application database, reached with Entra authentication. |
| Key Vault | Connection string, JWT signing key and seed admin password. |
| Static Web App | Hosts the SPA (Free tier) and calls the API directly, with CORS. |
| Log Analytics workspace | Console and system logs of the environment. |

All of them live in the `rg-ticketbooking` resource group. The images come from GHCR, where they are
public, so the Container App and the job need no registry credentials.

## Configuration

The passwordless connection string stored in the vault (`ConnectionStrings--DefaultConnection`):

```
Server=tcp:<server>.database.windows.net,1433;Initial Catalog=TicketBookingPlatform;Authentication=Active Directory Managed Identity;Encrypt=True;TrustServerCertificate=False;
```

With a system-assigned identity no `User Id` is needed; each container connects as its own identity,
which is how the API and the migrator get different database permissions from the same connection
string. No code change is involved: the managed identity authentication is a connection string option
of `Microsoft.Data.SqlClient`.

The app reloads the vault every 30 minutes. After changing a secret, restart the revision (or create a
new one) to apply it immediately.

The secrets come from the vault, so the environment variables only hold non-secret settings:

| Where | Environment variables |
|---|---|
| Container App | `KeyVault__Uri`, `ASPNETCORE_ENVIRONMENT=Production`, `ForwardedHeaders__Enabled=true`, `Https__RedirectEnabled=false`, `Https__HstsEnabled=false` (TLS terminates at the ingress), `Cors__AllowedOrigins__0` (see [SPA and CORS](#spa-and-cors)) |
| Migrator job | `KeyVault__Uri`, `Seed__Admin__Email`; optionally `Migrator__SeedDemoData=true` for the demo data |

See [Configuration](configuration.md) for every setting and
[Azure Key Vault](configuration.md#azure-key-vault) for the secret naming.

## Deploying the API

The **API deploy workflow** (`.github/workflows/deploy-api.yml`) signs in to Azure with **OIDC** (a
federated credential), so no client secret is stored in GitHub. The job runs in the `production` GitHub Environment, which is also
what the federated credential trusts. Add *required reviewers* to that environment if you ever want a
manual approval step before a deploy.

`Contributor` on the resource group covers everything the workflow does: it updates the job image
(`Microsoft.App/jobs/write`), starts the job **without** container overrides
(`Microsoft.App/jobs/start/action`) and updates the Container App.

Prerequisites in the GitHub repository (all three are
**environment secrets** of `production`, like the Static Web Apps token below):

| Kind | Name | Value |
|---|---|---|
| Environment secret | `AZURE_CLIENT_ID` | The `appId` of the app registration used for the OIDC login. |
| Environment secret | `AZURE_TENANT_ID` | The Entra tenant id. |
| Environment secret | `AZURE_SUBSCRIPTION_ID` | The subscription that holds `rg-ticketbooking`. |

The resource names (`rg-ticketbooking`, `containerapp-ticketbooking-api`,
`job-ticketbooking-migrator`) are set in the workflow's `env` block.

## Probes

Map the [health endpoints](observability.md#health-endpoints) to the container probes
(`/health/live` for liveness and startup, `/health/ready` for readiness, port `8080`). Give the startup
probe a generous window (an initial delay of about 10 s and a failure threshold of about 30 work
well): the app needs a few seconds to read the vault and warm up, and the first `/health/ready` call
also pays for acquiring the Managed Identity token. "Probe of StartUp failed" entries before that are
just noise.

## SPA and CORS

The SPA is hosted on Azure Static Web Apps and calls the Container App **directly**, so the two run
on different origins and CORS is required. Two settings have to match:

1. The SPA's [`config.json`](configuration.md#spa-runtime-configuration) — `apiBasePath` set to the
   Container App URL.
2. The API — `Cors__AllowedOrigins__0` set to the Static Web Apps URL.

```
Cors__AllowedOrigins__0 = https://<spa-name>.azurestaticapps.net
```

The CORS middleware is only registered when the list is non-empty, which is why local development
and Docker Compose (where nginx proxies `/api/` on the same origin) need no CORS configuration at
all. Every additional origin — a custom domain or a staging slot — needs its own indexed entry
(`Cors__AllowedOrigins__1`, ...); the policy uses `AllowCredentials`, so wildcards are not allowed.

> Two alternatives would avoid CORS entirely, both rejected for now:
>
> - The Static Web Apps *linked backend* feature, which proxies the API under the SPA origin. It
>   requires the Standard plan.
> - Running the existing SPA image (nginx with the `/api/` proxy) as a second Container App. The API
>   ingress could then even be internal, but the SPA would lose the global CDN of Static Web Apps and
>   gain a cold start, which is not worth it for static content.

## Deploying the SPA

The **SPA deploy workflow** (`.github/workflows/deploy-spa.yml`) builds the Angular application from
source and uploads it to Static Web Apps; the SPA image from GHCR is not used in Azure. On a `v*.*.*` tag it is called from `ci.yml`
once the API deploy has succeeded; it can also be run on demand via *Run workflow*.

The workflow writes `config.json` into the build output from the `API_BASE_PATH` variable (a trailing
`/` is removed), so the same source produces the right API URL per environment. A manual run can
override it with the `api_base_path` input; if neither is set, the workflow fails instead of shipping
an SPA without an API URL.

Prerequisites in the GitHub repository:

| Kind | Name | Value |
|---|---|---|
| Environment secret (`production`) | `AZURE_STATIC_WEB_APPS_API_TOKEN` | The deployment token from the Static Web App (*Overview → Manage deployment token*). |
| Environment variable (`production`) | `API_BASE_PATH` | The Container App URL, e.g. `https://<app>.<region>.azurecontainerapps.io`. |

`public/staticwebapp.config.json` handles the routing: unknown paths fall back to `index.html` (the
Angular router owns them) and `config.json` is served with `Cache-Control: no-store`. It also adds
basic security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`) to every
response.

When creating the Static Web App in the portal, choose **Deployment source: Other** — the workflow
above already does the build and upload, so Azure should not generate its own.
