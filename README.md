# TestApp

A learning project: a .NET 10 minimal API and an Angular 22 SPA, both deployed to
Azure Container Apps via GitHub Actions.

| | API | Frontend |
|---|---|---|
| Source | [`TestApp/`](TestApp/) | [`testapp-frontend/`](testapp-frontend/) |
| Live | https://testapp.mangocoast-d590f589.northeurope.azurecontainerapps.io | https://testapp-frontend.mangocoast-d590f589.northeurope.azurecontainerapps.io |
| Image | `ghcr.io/geostatie/testapp-api` | `ghcr.io/geostatie/testapp-frontend` |

The API exposes `GET /clinics`, `GET /clinics/{id}/doctors` and Swagger at
`/swagger`. Clinics and doctors live in SQL (Azure SQL in Azure, a SQL Server
container locally), accessed through EF Core. Its root path
`/` is deliberately unmapped, so a bare URL returns an empty 404 — that is the
app answering, not a broken deployment.

## Running locally

### Everything at once (Aspire)

[`TestApp.AppHost/`](TestApp.AppHost/) starts the API, the worker, the Angular
dev server, a **SQL Server** container and a local **Service Bus emulator**
together, and opens the Aspire dashboard (logs, status and URLs for every part):

```bash
dotnet run --project TestApp.AppHost    # or set TestApp.AppHost as the startup project and press F5
```

Requires Docker Desktop running and the .NET 10 SDK. The emulator gets its own
`reports` queue, so local runs never touch the queue in Azure. The first run
pulls about 2 GB of images. If the `servicebus` resource fails on that first run,
restart it from the dashboard: its SQL dependency was still downloading.

Every run starts from an empty database: the API applies its EF Core migrations
on startup, which create the tables and insert the example clinics and doctors.

### API and frontend only

Two terminals. The API refuses to start without a database, so point it at a
SQL Server you run yourself first:

```bash
dotnet user-secrets set "ConnectionStrings:clinicdb" "<connection string>" --project TestApp
dotnet run --project TestApp/TestApp.csproj --launch-profile http   # http://localhost:5280
cd testapp-frontend && npm start                                    # http://localhost:4200
```

`appsettings.Development.json` already allows `http://localhost:4200` as a CORS
origin, and [`testapp-frontend/public/config.json`](testapp-frontend/public/config.json)
points the SPA at `http://localhost:5280`.

Requires **Node >= 24.15.0** (Angular 22's engine constraint).

## How configuration works

The API URL is **never compiled into the Angular bundle**. The app fetches
`/config.json` before it bootstraps, and the container entrypoint rewrites that
file from the `API_BASE_URL` environment variable on startup. One image
therefore runs against any environment, and a missing `API_BASE_URL` fails the
container immediately rather than silently serving a wrong URL.

The API's database connection is `ConnectionStrings:clinicdb`. The AppHost sets
it locally; in Azure it is the env var `ConnectionStrings__clinicdb`, sourced from
the container app secret `sql-clinicdb`.

The API's allowed CORS origins come from configuration
(`Cors:AllowedOrigins`), supplied in Azure as `Cors__AllowedOrigins__0`. It is an
allowlist, not a wildcard: an unlisted origin receives no CORS header at all.

## Database changes

The schema is defined in code ([`TestApp/Data/`](TestApp/Data/)) and changed
through EF Core migrations, which are committed and applied by the API on
startup. After changing an entity:

```bash
dotnet tool restore                                                   # once: installs dotnet-ef from dotnet-tools.json
dotnet ef migrations add <Name> --project TestApp --output-dir Data/Migrations
```

Read the generated migration before committing it: it is the exact change that
will run against Azure SQL on the next deployment.

## Deployment

Pushing to `main` triggers [`api.yml`](.github/workflows/api.yml) or
[`frontend.yml`](.github/workflows/frontend.yml) depending on which paths
changed. Each builds an image, pushes it to GHCR tagged with the commit SHA, and
updates the container app. Azure authentication uses OIDC federated credentials,
so no secrets are stored — the identity is Contributor scoped to `testapp-rg`
alone.

To roll back, re-deploy an earlier SHA tag:

```bash
az containerapp update -n testapp-frontend -g testapp-rg \
  --image ghcr.io/geostatie/testapp-frontend:<older-sha>
```

## Cost

Both apps run at `minReplicas 0` and cost nothing while idle. The Container Apps
free grant (180,000 vCPU-s, 360,000 GiB-s, 2M requests per month) is **per
subscription and shared between both apps**, not per app. Raising `minReplicas`
to 1 to avoid cold starts is the change that would produce a real bill.

The database uses the Azure SQL **free offer** (100,000 vCore-seconds and 32 GB
per month) with `--free-limit-exhaustion-behavior AutoPause`: if the monthly
allowance runs out, the database pauses until the next month instead of
billing. It also auto-pauses when idle, so the first request after a quiet
period waits for both the API to cold-start and the database to resume, which
can take up to a minute. EF Core's retry-on-failure covers the resume.
