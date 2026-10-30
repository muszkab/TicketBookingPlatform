# Ticket Booking Platform

A demo event ticketing platform built as a portfolio project to showcase **.NET backend
engineering with Clean Architecture**, exposing a versioned **REST API** consumed by an
**Angular SPA** through a generated OpenAPI client.

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
