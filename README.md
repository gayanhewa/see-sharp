# See Sharp

A small invoicing and expense tracker for freelancers, built as a learning
reference for a modern C# REST API. Track clients, raise invoices with a
guarded status lifecycle, record expenses by category, and see a monthly
summary of income versus expenses.

The point of the project is to be read. It shows clean-ish architecture,
a rich domain model, EF Core with PostgreSQL, full OpenTelemetry into a
self-hosted SigNoz stack, and a small React frontend, without framework
ceremony getting in the way.

## Prerequisites

- .NET 10 SDK: `brew install --cask dotnet-sdk`
- Node (current LTS): `brew install node`
- Docker Desktop (for Postgres, Testcontainers, SigNoz, and the containerized run)

Verify:

```bash
dotnet --version   # 10.x
node --version     # v20 or newer
docker --version
```

## Layout

```
api/      C# solution: Domain, Application, Infrastructure, Api, tests
web/      React SPA (Vite, TypeScript, React Router)
deploy/   docker-compose for the app stack, SigNoz via Foundry
docs/     architecture tour
```

## Quick start

1. Start Postgres:

   ```bash
   docker compose -f deploy/docker-compose.yml up -d postgres
   ```

2. Run the API (applies migrations and seeds demo data on startup):

   ```bash
   dotnet run --project api/SeeSharp.Api
   ```

   The API listens on http://localhost:5080. Swagger UI is at
   http://localhost:5080/swagger. Every endpoint needs the header
   `Authorization: Bearer dev-secret-token`.

3. Run the web app:

   ```bash
   cd web
   npm install
   npm run dev
   ```

   Open http://localhost:5173. The Vite dev server proxies API calls, so no
   extra config is needed.

## Seeing the telemetry

Start the SigNoz stack (managed by Foundry, the official SigNoz CLI):

```bash
foundryctl cast -f deploy/signoz/casting.yaml
```

Then generate traffic against the API and open http://localhost:8080. Look
for the `see-sharp-api` service: HTTP request traces with EF Core child
spans, a hand-rolled `invoice.status_change` span, the `invoices_created`
counter, runtime metrics, and logs. See `deploy/signoz/README.md` for
details. The API works fine with SigNoz down; exports just fail quietly.

## Fully containerized run

```bash
docker compose -f deploy/docker-compose.yml up --build
```

This runs Postgres, the API (on 5080), and the web app (on 8081) together.
The API in Docker points its telemetry at the SigNoz ingester on the host.

## Tests

```bash
cd api
dotnet test
```

Runs the domain unit tests and the API integration tests. The integration
tests use Testcontainers, so Docker must be running.

The SPA has Playwright end-to-end tests. They need the API and Postgres
running (the quick start above covers that), plus a one-time browser install:

```bash
cd web
npx playwright install chromium
npm run test:e2e
```

## Ports

| Port | What |
| --- | --- |
| 5080 | API |
| 5173 | Vite dev server |
| 8081 | Containerized web |
| 5432 | PostgreSQL |
| 8080 | SigNoz UI |
| 4317 / 4318 | SigNoz OTLP ingester |

## Learn more

`docs/ARCHITECTURE.md` is a guided tour of every project, file, and request
path.
