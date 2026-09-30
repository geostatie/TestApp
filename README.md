# TestApp

A learning project: a .NET 9 minimal API and an Angular 22 SPA, both deployed to
Azure Container Apps via GitHub Actions.

| | API | Frontend |
|---|---|---|
| Source | [`TestApp/`](TestApp/) | [`testapp-frontend/`](testapp-frontend/) |
| Live | https://testapp.mangocoast-d590f589.northeurope.azurecontainerapps.io | https://testapp-frontend.mangocoast-d590f589.northeurope.azurecontainerapps.io |
| Image | `ghcr.io/geostatie/testapp-api` | `ghcr.io/geostatie/testapp-frontend` |

The API exposes `GET /weatherforecast` and Swagger at `/swagger`. Its root path
`/` is deliberately unmapped, so a bare URL returns an empty 404 — that is the
app answering, not a broken deployment.

## Running locally

Two terminals. Use the **http** launch profile so no HTTPS dev certificate is
needed:

```bash
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

The API's allowed CORS origins come from configuration
(`Cors:AllowedOrigins`), supplied in Azure as `Cors__AllowedOrigins__0`. It is an
allowlist, not a wildcard: an unlisted origin receives no CORS header at all.

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
