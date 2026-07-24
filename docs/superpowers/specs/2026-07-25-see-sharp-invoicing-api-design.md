# See Sharp: Invoicing and Expense Tracker Reference Project

Design spec. Written 2026-07-25.

## Purpose

This is a learning reference project for building a REST API in C#. The goal is a
working end-to-end system a developer can run, read, and come back to. It should
demonstrate modern C# practices, real observability, and a clean way to isolate
business rules, without so much abstraction that the fundamentals get buried.

The domain is a small invoicing and expense tracker aimed at freelancers and solo
operators: track clients, raise invoices with line items, move invoices through a
status lifecycle, record expenses against categories, and see a monthly summary of
income versus expenses.

## What we are building

Three moving parts plus observability:

- A standalone C# REST API (the focus of the project).
- A React single-page app that consumes the API over HTTP.
- PostgreSQL for storage.
- Full OpenTelemetry instrumentation exporting to a self-hosted SigNoz stack.
- Docker for the dependencies and for a fully containerized run when wanted.

```
  React SPA  --HTTP/JSON-->  C# REST API  --EF Core-->  PostgreSQL
 (Vite + TS)               (Clean-ish, .NET 10)
                                  |
                                  | OTLP (traces / metrics / logs)
                                  v
                             SigNoz stack
                    (ClickHouse, OTel collector,
                       query service, web UI)
```

## Architecture: Clean-ish

The API uses a light version of Clean Architecture. Four projects, with
dependencies pointing inward. Inner layers know nothing about outer layers. The
point is to keep business rules free of infrastructure concerns so they can be
tested in isolation and cannot be bypassed.

```
SeeSharp.Api  -->  SeeSharp.Infrastructure  -->  SeeSharp.Application  -->  SeeSharp.Domain
  (HTTP,             (EF Core, Postgres,            (use cases, DTOs,          (entities +
   DI wiring,         OpenTelemetry, migrations)     interfaces, validation)    rules, no deps)
   endpoints)
```

"Clean-ish" means we keep the layer separation and the rich domain, but skip the
heavier machinery that a small project does not need:

- No MediatR or CQRS. Use cases are plain handler classes.
- No AutoMapper. Mapping between DTOs and entities is done by hand, which is easy
  to read and easy to follow.
- No repository interfaces layered on top of EF Core. EF Core's `DbContext` is
  already a unit of work, so the Application layer depends on a single
  `IAppDbContext` interface rather than a repository per entity.

### SeeSharp.Domain

Pure C# with zero dependencies. No EF Core, no ASP.NET. This is where the business
rules live and where they are protected.

- Entities: `Client`, `Invoice`, `InvoiceLineItem`, `Expense`, `Category`.
- Entities use private setters and expose methods that enforce rules, for example
  `Invoice.MarkAsSent()`, `Invoice.MarkAsPaid()`, `Invoice.AddLineItem(...)`, and a
  computed `Total`.
- `InvoiceStatus` enum: `Draft`, `Sent`, `Paid`, `Overdue`, `Cancelled`.
- A `Money` value object that guards against negative amounts.
- Domain exceptions such as `InvalidInvoiceTransitionException`.

The status transition rule is the clearest example of isolation. Because the setter
is private and the rule lives on the entity, an endpoint cannot put an invoice into
an illegal state:

```csharp
public void MarkAsPaid()
{
    if (Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
        throw new InvalidInvoiceTransitionException(Status, InvoiceStatus.Paid);
    Status = InvoiceStatus.Paid;
}
```

### SeeSharp.Application

Depends on Domain only. Describes what the app does.

- Use-case handlers per feature as plain classes, for example
  `CreateInvoiceHandler`, `ChangeInvoiceStatusHandler`, `GetInvoicesHandler`.
- Request and response DTOs as `record` types. EF entities are never exposed
  directly over HTTP.
- FluentValidation validators for input.
- `PagedResult<T>` for paginated responses.
- One infrastructure interface, `IAppDbContext`, so the Application layer can query
  without referencing EF Core.

### SeeSharp.Infrastructure

Depends on Application and Domain. Implements the interfaces with real technology.

- `AppDbContext : DbContext, IAppDbContext` using EF Core with Npgsql for Postgres.
- Entity type configurations, including money mapped to `numeric(18,2)`.
- EF Core migrations, committed to the repo.
- Seed data for first run.
- OpenTelemetry setup as extension methods.

### SeeSharp.Api

Depends on all the others, but only to wire them together and expose HTTP.

- Feature-grouped Minimal API endpoints, kept thin: parse the request, call an
  Application handler, map the result, return it.
- `Program.cs` registers everything in the DI container.
- Hardcoded single-account auth middleware (see below).
- Global exception handling that returns RFC 9457 `ProblemDetails`.
- Swagger / OpenAPI UI for exploring and testing the API.

## Data model

```
Client  ---<  Invoice  ---<  InvoiceLineItem
                 |
Category ---<  Expense
```

- `Client`: Id, Name, Email, Address, CreatedAt.
- `Invoice`: Id, ClientId (FK), Number, Status, IssueDate, DueDate, Notes,
  CreatedAt. Total is computed from line items, not stored.
- `InvoiceLineItem`: Id, InvoiceId (FK), Description, Quantity, UnitPrice.
- `Expense`: Id, CategoryId (FK, nullable), Description, Amount, Date, Vendor,
  CreatedAt.
- `Category`: Id, Name.

Money is always `decimal` in C# and `numeric(18,2)` in Postgres. Never `double`.
This is a deliberate teaching point.

## API surface

| Resource   | Endpoints |
|------------|-----------|
| Clients    | `GET /clients` (paged), `GET /clients/{id}`, `POST`, `PUT`, `DELETE` |
| Invoices   | `GET /invoices` (paged, filter by status and client), `GET /invoices/{id}`, `POST`, `PUT`, `DELETE`, `POST /invoices/{id}/status` |
| Expenses   | `GET /expenses` (paged, filter by category and date range), `GET /expenses/{id}`, `POST`, `PUT`, `DELETE` |
| Categories | `GET /categories`, `POST`, `DELETE` |
| Reports    | `GET /reports/summary?from=&to=` (income, expenses, and net per month) |

Cross-cutting concerns:

- DTOs for every request and response.
- FluentValidation, surfaced as `ProblemDetails`.
- Global exception handling returning consistent `ProblemDetails`.
- Pagination via `PagedResult<T>` and `page` / `pageSize` query params.
- Hardcoded single-account auth: one token read from config, checked by middleware
  on every request. Deliberately simple and swappable for real auth later.
- EF Core migrations applied automatically on startup in development.
- Seed data on first run so the UI and the traces have something to show.

## Telemetry

OpenTelemetry .NET SDK in the API, exporting everything over OTLP to the SigNoz
collector. Vendor-neutral: nothing in the app code names SigNoz beyond an endpoint
URL in config.

- Traces: auto-instrument incoming HTTP requests (ASP.NET Core) and outgoing EF
  Core database calls, so each request produces a span tree with a child span per
  SQL query. Add a small number of manual spans in the domain use cases, for
  example an `invoice.status_change` span, to show how custom spans are created by
  hand.
- Metrics: ASP.NET Core request metrics, HTTP client metrics, and .NET runtime
  metrics (GC, thread pool, allocations). Add one custom counter, invoices created,
  to show the manual path.
- Logs: route the standard `ILogger` through OpenTelemetry so logs export via OTLP
  and carry the active trace ID. In SigNoz you can then jump from a log line to its
  trace.

Setup lives in `SeeSharp.Infrastructure/Telemetry/` as an `AddSeeSharpTelemetry`
extension method, called once from `Program.cs`. Configuration (OTLP endpoint,
service name, sampling) comes from `appsettings.json` and environment variables, so
the same build points at SigNoz in Docker or at a local collector without code
changes. Service name is `see-sharp-api`, with resource attributes for version and
environment.

SigNoz runs from its official Docker Compose in `deploy/signoz/` as its own stack
(ClickHouse, OTel collector, query service, web UI). The exporter points at the
collector on 4317 (gRPC) or 4318 (HTTP). When SigNoz is not running the exporter
drops data and the app keeps working, so telemetry never blocks development.

## Frontend

`web/`, built with Vite, React, TypeScript, and React Router. Clean but modest so
the API stays the focus.

- Routes: Dashboard (monthly summary from `/reports/summary`), Clients (list plus
  create and edit), Invoices (list with status filter, detail with line items,
  status-change action), Expenses (list with category and date filter, create and
  edit).
- A thin typed API client: one `fetch` wrapper that attaches the auth token, sets
  JSON headers, and throws typed errors from `ProblemDetails` responses. TypeScript
  interfaces mirror the API DTOs.
- Data fetching with plain `fetch` and React state or small hooks. No Redux. If it
  grows, TanStack Query is the natural next step and the docs will say so rather
  than pulling it in now.
- Minimal hand-written styling, enough to look tidy without becoming a design
  project.
- The auth token is entered once or injected via config, stored client-side, and
  sent on every request.

## Runtime and Docker

Development is SDK-first, with Docker providing the dependencies.

Day to day:

- `dotnet run` or `dotnet watch` for the API, hot-reloading on save.
- `npm run dev` for the React app, with Vite HMR proxying to the running API.
- `docker compose up` in `deploy/` for Postgres.
- SigNoz brought up from `deploy/signoz/` when observing.

Dockerfiles ship so the whole thing can run containerized too:

- `api/Dockerfile`: multi-stage .NET build (SDK image builds and publishes, runtime
  image runs the output) for a small final image.
- `web/Dockerfile`: multi-stage Node build, static files served by nginx.
- `deploy/docker-compose.yml`: Postgres, API, and web wired together, with the API
  pointed at Postgres and the SigNoz collector via environment variables.

Configuration:

- Connection strings, the auth token, and the OTLP endpoint come from environment
  variables with sensible defaults in `appsettings.Development.json`.
- A committed `.env.example` documents every variable. The real `.env` stays
  gitignored.

Ports (to be confirmed against SigNoz defaults during scaffolding, and listed in
the README): API on 5080, Vite dev server on 5173, Postgres on 5432, SigNoz UI on
3301.

## Testing

xUnit throughout.

- Domain unit tests are the centerpiece and the payoff of the layer separation.
  Because the domain has no dependencies, the rules are tested with plain objects
  and no database: legal and illegal invoice status transitions, total computation
  from line items, the `Money` value object rejecting negatives, line-item math.
- Application handler tests for the use cases, using a test `IAppDbContext` where it
  helps.
- API integration tests using `WebApplicationFactory` against a real Postgres in a
  container via Testcontainers, covering happy paths and validation and error
  responses for each feature.

## Tooling and conventions

- .NET 10 (current LTS, installed via Homebrew), C# with nullable reference types on
  and warnings as errors for the app projects.
- `.editorconfig` covering C# and TypeScript.
- `dotnet format` for C#, Prettier and ESLint for the frontend.
- `record` DTOs, file-scoped namespaces, primary constructors where they read well,
  used where they genuinely help.
- EF Core migrations committed and applied on startup in development.

## Repository layout

```
see-sharp/
  api/
    SeeSharp.Domain/
    SeeSharp.Application/
    SeeSharp.Infrastructure/
    SeeSharp.Api/
    SeeSharp.Domain.Tests/
    SeeSharp.Api.Tests/
    SeeSharp.sln
  web/                      React app (Vite + TS + React Router)
  deploy/
    docker-compose.yml      Postgres + API + web
    signoz/                 SigNoz official compose and config
  docs/
    ARCHITECTURE.md         guided tour and project map
    superpowers/specs/      this design doc
  .editorconfig
  .env.example
  .gitignore
  README.md
```

## Deliverables

- The `api/` Clean-ish solution (Domain, Application, Infrastructure, Api) plus test
  projects, building and running.
- The `web/` React app consuming the API.
- `deploy/` with the app compose file, the SigNoz stack, and Dockerfiles.
- `docs/ARCHITECTURE.md`: a guided-tour project map. Every folder and file
  explained, the request lifecycle traced end to end (for example `POST /invoices`
  from routing through validation, handler, EF Core, Postgres, and back, and where a
  span is created at each hop), the data model, the telemetry flow, and the layer
  dependency rule with an explanation of why the domain has no dependencies. Written
  in a plain human voice.
- `README.md`: prerequisites and install steps (Homebrew install of the .NET 10 SDK
  and Node, neither of which is currently installed), then the exact commands to
  bring up Postgres and SigNoz, run the API and web, seed data, open Swagger, and
  open SigNoz to view traces.
- `.env.example`, `.gitignore`, `.editorconfig`, and a clean initial git history.

## Step zero

The machine does not currently have the .NET SDK or Node installed. The first
implementation step is installing them with Homebrew (`brew install --cask
dotnet-sdk` for .NET 10, plus Node), then confirming `dotnet --version` and
`node --version` before scaffolding anything.

## Out of scope for v1

- Multi-user accounts and real authentication (single hardcoded account only).
- PDF invoice generation and emailing.
- Payments or third-party integrations.
- Deployment to any cloud host. This runs locally.
