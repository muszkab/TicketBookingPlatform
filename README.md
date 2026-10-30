# Ticket Booking Platform

A demo event ticketing platform built as a portfolio project to showcase **.NET backend
engineering with Clean Architecture**, exposing a versioned **REST API** consumed by an
**Angular SPA**, deployed to the **Azure cloud**.

**Live demo:** [ticketbooking.muszkab.com](https://ticketbooking.muszkab.com)

## Tech stack

### Backend

| Area | Technology |
|---|---|
| Framework | .NET 9 |
| Web | ASP.NET Core Web API|
| Persistence | Entity Framework Core|
| Database | SQL Server |
| Architecture | Clean Architecture|
| API | REST API, OpenAPI + Scalar UI |

### Frontend

| Area | Technology |
|---|---|
| Framework | Angular 22 |
| UI | Angular Material |
| Language / styling | TypeScript, SCSS |
| API client | Generated from OpenAPI |

### Cloud & DevOps

| Area | Technology |
|---|---|
| API hosting | Azure Container Apps |
| SPA hosting | Azure Static Web Apps |
| Database | Azure SQL |
| Migration | Azure Container Apps Job (dedicated migrator) |
| Secrets | Azure Key Vault |
| CI | GitHub Actions, images published to GHCR |
| CD | Automated deployment to Azure on release tag |
| Containers | Docker, Docker Compose |

---

## Roadmap

- [ ] Upgrade to .NET 10 (LTS)
- [x] Docker Compose (SQL Server + API + SPA)
- [ ] FluentValidation for command/query input validation
- [ ] Testcontainers (real SQL Server) for the integration tests
- [x] CI/CD pipeline (build & test, Docker Compose smoke test, GHCR image publish, automated Azure deploy)
- [ ] Re-enable the OpenAPI drift check
- [x] Cloud-native readiness (health checks, OpenTelemetry, externalised configuration)
- [x] Deployment to Azure (Container Apps + Azure SQL + Static Web Apps, secrets in Key Vault)

---

## Documentation

The full documentation lives in [docs/](docs/README.md):

- [Getting started](docs/getting-started.md): prerequisites, running locally or with Docker Compose,
  published images, common tasks, troubleshooting
- [Architecture](docs/architecture.md): detailed tech stack, layers and dependencies, project structure, key
  technical decisions, feature overview
- [Configuration](docs/configuration.md): configuration sources, all settings with defaults, Docker Compose
  `.env`, Azure Key Vault, SPA runtime configuration
- [Health & observability](docs/observability.md): health endpoints, OpenTelemetry, error correlation,
  Aspire Dashboard
- [CI/CD](docs/ci-cd.md): pipeline overview, CI jobs, Dependabot, versioning and image tags, releasing a
  new version
- [Azure deployment](docs/azure-deployment.md): resources, identities and database permissions,
  configuration, deploying the API (OIDC) and the SPA, probes, CORS
- [Known limitations](docs/known-limitations.md): deliberate shortcuts and open items (security,
  operations, features)
