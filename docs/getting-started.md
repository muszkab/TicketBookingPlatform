# Getting started

There are two ways to run the platform:

- **[Locally](#run-locally)**: the API with `dotnet run`, the SPA with the Angular dev server. Best for
  development (hot reload, debugger).
- **[With Docker Compose](#run-with-docker-compose)**: SQL Server, migrator, API and SPA in containers,
  close to the production setup. Only Docker is needed.

## Prerequisites

| Tool | Needed for | Notes |
|---|---|---|
| .NET SDK 9.0+ | API, tests | |
| SQL Server LocalDB | API (locally) | Windows only. Any other SQL Server works too, see [Using another SQL Server](#using-another-sql-server). |
| Node.js 24 | SPA | The Angular CLI comes with the project, no global install needed. |
| [mkcert](https://github.com/FiloSottile/mkcert) | SPA dev server | Creates the locally trusted HTTPS certificate, see [step 2](#2-trust-the-https-certificates). |
| Docker | Docker Compose | Optional. |
| Java 11+ | `npm run generate:api` | Optional, only to regenerate the API client (`openapi-generator-cli`). |

## Run locally

### 1. Clone the repository

```powershell
git clone https://github.com/muszkab/TicketBookingPlatform.git
cd TicketBookingPlatform
```

### 2. Trust the HTTPS certificates

Both the API (`https://localhost:5001`) and the SPA dev server (`https://localhost:4200`) run on HTTPS.

```powershell
dotnet dev-certs https --trust

cd frontend/ticket-booking-web
mkcert -install
mkdir ssl
mkcert -cert-file ssl/localhost+2.pem -key-file ssl/localhost+2-key.pem localhost 127.0.0.1 ::1
cd ../..
```

The `ssl/` folder is git-ignored; `angular.json` expects exactly these two file names.

### 3. Configure the secrets

```powershell
dotnet user-secrets --project src/WebApi set "JwtSettings:SigningKey" "<at-least-32-character-random-key>"
dotnet user-secrets --project src/WebApi set "Seed:Admin:Email" "admin@example.com"
dotnet user-secrets --project src/WebApi set "Seed:Admin:Password" "<strong-password>"
```

The API refuses to start without these. Every other setting has a working default for local
development, see [Configuration](documentation.md#configuration).

### 4. Run the API

```powershell
dotnet run --project src/WebApi
```

In Development the API creates the `TicketBookingPlatform` database on LocalDB, applies the migrations
and seeds the administrator plus the demo locations, events and ticket categories on startup.
`--launch-profile Scalar` also opens the API reference in the browser.

| Endpoint | URL |
|---|---|
| API | `https://localhost:5001/api/v1/...` |
| Scalar API reference | `https://localhost:5001/scalar` |
| OpenAPI documents | `https://localhost:5001/openapi/v1.json`, `/openapi/v2.json` |
| Build & runtime info | `https://localhost:5001/api/v1/meta` (anonymous, feeds the SPA footer) |
| Health | `https://localhost:5001/health/live`, `/health/ready` |

### 5. Run the SPA

In a second terminal:

```powershell
cd frontend/ticket-booking-web
npm ci
npm start
```

Open `https://localhost:4200`. The dev server calls the API at `https://localhost:5001` directly; the
API already allows this origin (`Cors:AllowedOrigins` in `appsettings.Development.json`).

### 6. Sign in

Sign in with the administrator from step 3, or register a new account in the SPA (new accounts get the
`Customer` role).

### 7. Run the tests

```powershell
dotnet test                                     # backend, no database or Docker needed
cd frontend/ticket-booking-web; npm test        # frontend (Vitest)
```

The backend tests use EF Core InMemory and SQLite, so they run without SQL Server.

### Using another SQL Server

Without LocalDB (e.g. on macOS or Linux), point the API at any SQL Server instance:

```powershell
dotnet user-secrets --project src/WebApi set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=TicketBookingPlatform;User Id=sa;Password=<password>;TrustServerCertificate=True"
```

## Run with Docker Compose

```powershell
Copy-Item .env.example .env   # then replace the placeholder secrets
docker compose up -d --build
```

Keep `--build`: without it Compose silently reuses an existing local image, even after code changes.

The services start in order: SQL Server, then the one-shot `migrator` (applies the migrations and seeds
the administrator and the demo data), and only after it has finished successfully the API, then the
SPA. The API itself never migrates in containers, so it stays safe to scale out.

| Service | URL |
|---|---|
| SPA | `http://localhost:4200` (`SPA_PORT`) |
| API | `http://localhost:8080/api/v1/...` (`API_PORT`) |
| Scalar API reference | `http://localhost:8080/scalar` |

Sign in with `SEED_ADMIN_EMAIL` / `SEED_ADMIN_PASSWORD` from `.env`.

If `npm start` is already running on port 4200, set another port first (`SPA_PORT=4300` in `.env`).

SQL Server publishes no host port. `db/backup` is mounted read-only into the container at
`/var/opt/mssql/backup`, so an existing `.bak` can be restored from there. When restoring a backup, set
`SEED_ADMIN_EMAIL` to an administrator that already exists in it, otherwise a second one is created.

To collect traces, metrics and logs locally, start the optional Aspire Dashboard as well, see
[OpenTelemetry](documentation.md#opentelemetry).

Stop the stack with `docker compose down` (add `-v` to delete the database volume too).

### Run the published images

To run a released version instead of building locally:

```powershell
$env:IMAGE_TAG = "2.4.0"   # any published semver tag, e.g. 2.4.0, 2.4 or 2
docker compose -f docker-compose.yml -f docker-compose.publishedimage.yml up -d
```

See [Versioning and image tags](documentation.md#versioning-and-image-tags) for the available tags.

## Common tasks

Regenerate the TypeScript API client after an API change (the API must be running on
`https://localhost:5001`, and Java is required):

```powershell
cd frontend/ticket-booking-web
npm run refresh:api   # downloads openapi/v1.json, then regenerates src/app/api
```

Add an EF Core migration (requires the `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`):

```powershell
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/WebApi
```

## Troubleshooting

| Symptom | Fix |
|---|---|
| `npm start` fails with `ENOENT ... ssl/localhost+2.pem` | The SPA certificate is missing, see [step 2](#2-trust-the-https-certificates). |
| The SPA loads, but every API call fails | The API is not running, or its certificate is not trusted: run `dotnet dev-certs https --trust` and open `https://localhost:5001/health/live` once in the browser. |
| The API stops at startup with a configuration error | A required secret is missing, see [step 3](#3-configure-the-secrets). |
| A Compose service is `unhealthy` or runs old code | Rebuild with `docker compose up -d --build`. |
| `docker compose up` fails with a port conflict | Change `API_PORT` / `SPA_PORT` in `.env`. |
