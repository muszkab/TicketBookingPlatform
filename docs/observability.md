# Health & observability

## Health endpoints

| Endpoint | Check | Purpose |
|---|---|---|
| `/health/live` | `self` — the process is running, no dependencies | Liveness: is the process alive? Restarting is the only cure if not. |
| `/health/ready` | `database` — EF Core can connect to SQL Server | Readiness: should this instance receive traffic? |

Both endpoints are anonymous and answer `200 Healthy` or `503 Unhealthy` as plain text. They live at
the API root, not under `/api/`:

| Where | URL | Polled by |
|---|---|---|
| Local (`dotnet run`) | `https://localhost:5001/health/live`, `/health/ready` | – |
| Docker Compose | `http://localhost:8080/health/live`, `/health/ready` (`API_PORT`) | The `api` container healthcheck (`/health/live`); the SPA container only starts once it is healthy. |
| Azure Container Apps | `https://<app>/health/live`, `/health/ready` | The liveness, startup and readiness probes, see [Probes](azure-deployment.md#probes). |

The CI smoke tests and the deploy workflow also wait for `/health/ready` before they continue.

The SPA container does not proxy these paths (nginx only forwards `/api/`), so they are reachable on
the API port only.

## OpenTelemetry

The API is instrumented with OpenTelemetry:

- **Traces** — incoming ASP.NET Core requests and EF Core queries.
- **Metrics** — ASP.NET Core and Kestrel metrics (request duration, active requests, connections) and
  .NET runtime metrics (GC, thread pool, exceptions).
- **Logs** — the existing `ILogger` output, including scopes and formatted messages. The console
  output stays as it is, OpenTelemetry is an additional destination.

Every signal carries the resource attributes `service.name = TicketBookingPlatform.WebApi` and
`service.version` (the assembly version, e.g. `2.4.1.0` for the `v2.4.1` release).

Telemetry is exported over OTLP (gRPC) to `OpenTelemetry:OtlpEndpoint`. When the setting is empty
nothing is exported, and the API runs exactly the same. The Azure setup does not set an endpoint, so
there only the console logs are collected, in the Log Analytics workspace.

### Correlating errors

The `GlobalExceptionHandler` adds a `traceId` to every `ProblemDetails` error response:

```json
{
  "title": "...",
  "status": 409,
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

The value is the W3C `traceparent` of the request: the 32 hexadecimal characters after `00-` are the
trace id, which finds the whole request (including its EF Core queries and logs) in the trace viewer.

## Viewing telemetry locally

Docker Compose has an optional Aspire Dashboard service that
receives OTLP and shows traces, metrics and structured logs. It is behind the `observability` profile,
so a plain `docker compose up` does not start it.

**With the containerised API**, set the endpoint in `.env` and start the stack with the profile:

```powershell
# .env
OTEL_EXPORTER_OTLP_ENDPOINT=http://aspire-dashboard:18889
```

```powershell
docker compose --profile observability up -d
# Dashboard: http://localhost:18888
```

**With the API running locally** (`dotnet run`), start the dashboard alone and point the API at its
published OTLP port:

```powershell
docker compose --profile observability up -d aspire-dashboard
dotnet user-secrets --project src/WebApi set "OpenTelemetry:OtlpEndpoint" "http://localhost:18889"
```

The dashboard runs without authentication (`DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS`), so it is for
local use only.

## Not instrumented yet

- **Outgoing HTTP calls.** `HttpClient` instrumentation is intentionally not wired up, because the API
  makes no outbound calls. It should be added with the first external integration (e.g. a payment
  provider).
- **The migrator.** It writes console logs only; its result is the exit code of the container or job.
- The health responses are plain text and the database check has no explicit timeout, see
  [Known limitations](documentation.md#known-limitations).
