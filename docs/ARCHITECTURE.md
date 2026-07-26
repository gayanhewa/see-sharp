# Architecture: a guided tour

See Sharp is a small invoicing and expense tracker for freelancers. It exists
to be read: a working end-to-end reference for a modern C# REST API with a
React frontend, PostgreSQL storage, and full OpenTelemetry into a self-hosted
SigNoz stack.

Three moving parts plus observability:

```
  React SPA  --HTTP/JSON-->  C# REST API  --EF Core-->  PostgreSQL
 (Vite + TS)               (Clean-ish, .NET 10)
                                  |
                                  | OTLP (traces / metrics / logs)
                                  v
                             SigNoz stack
```

## Top-level folders

| Folder | What it holds |
| --- | --- |
| `api/` | The C# solution: four projects, two test projects, the Dockerfile |
| `web/` | The React single-page app (Vite, TypeScript, React Router) |
| `deploy/` | Docker Compose for Postgres, the API, and the web, plus the SigNoz stack |
| `docs/` | Design spec, implementation plan, and this tour |

## The four API projects

Dependencies point inward, one direction only:

```
SeeSharp.Api  -->  SeeSharp.Infrastructure  -->  SeeSharp.Application  -->  SeeSharp.Domain
```

The Domain has no dependencies at all. No EF Core, no ASP.NET, no NuGet. That
is the point: business rules live in plain C#, so they can be tested without a
database or a web server, and they cannot be bypassed by an endpoint or a
query forgetting to check something. If an invoice cannot legally go from
Draft to Paid, there is no code path anywhere in the system that can make it
happen, because the guard lives in the entity itself.

### SeeSharp.Domain

Entities, value objects, enums, domain exceptions. Nothing else.

| File | Responsibility |
| --- | --- |
| `Entities/Invoice.cs` | The invoice aggregate. Status lifecycle (Draft, Sent, Paid, Overdue, Cancelled) with guarded transitions, line item management, computed total |
| `Entities/InvoiceLineItem.cs` | One line on an invoice. Validates quantity and price, computes its own line total |
| `Entities/Client.cs` | A client you bill. Name required, email and address optional |
| `Entities/Expense.cs` | A recorded expense. Amount validated through Money, optional vendor and category |
| `Entities/Category.cs` | An expense category |
| `ValueObjects/Money.cs` | Non-negative money as a record struct. Makes bad amounts unrepresentable |
| `Enums/InvoiceStatus.cs` | The five invoice states |
| `Exceptions/InvalidInvoiceTransitionException.cs` | Thrown when a status change breaks the lifecycle rules |

### SeeSharp.Application

Use cases. Plain static handler classes, hand-written DTO mapping,
FluentValidation validators. No MediatR, no AutoMapper, no repository
interfaces. Everything talks to one `IAppDbContext`.

| File | Responsibility |
| --- | --- |
| `Abstractions/IAppDbContext.cs` | The single persistence seam: five DbSets plus SaveChangesAsync |
| `Common/PagedResult.cs` | Paged list envelope with computed total pages |
| `Clients/ClientDtos.cs` | Client request and response records, entity to DTO mapping |
| `Clients/ClientValidators.cs` | Name required, email format when present |
| `Clients/ClientHandlers.cs` | Client CRUD plus paged list |
| `Categories/CategoryDtos.cs` | Category request and response records |
| `Categories/CategoryHandlers.cs` | Create, list, delete categories |
| `Expenses/ExpenseDtos.cs` | Expense request and response records |
| `Expenses/ExpenseValidators.cs` | Description required, amount not negative |
| `Expenses/ExpenseHandlers.cs` | Expense CRUD plus filtered, paged list |
| `Invoices/InvoiceDtos.cs` | Invoice, line item, and status change records |
| `Invoices/InvoiceValidators.cs` | Number, dates, and line item validation |
| `Invoices/InvoiceHandlers.cs` | Invoice CRUD, filtered paged list, status changes through the domain guards |
| `Reports/ReportDtos.cs` | Monthly summary row and summary response records |
| `Reports/ReportHandlers.cs` | Income versus expenses per month: paid invoices bucketed by issue month, expenses by date month |

### SeeSharp.Infrastructure

Persistence and telemetry. The only project that knows about Postgres.

| File | Responsibility |
| --- | --- |
| `Persistence/AppDbContext.cs` | EF Core context implementing IAppDbContext |
| `Persistence/Configurations.cs` | Table mappings: snake_case names, numeric(18,2) money, status stored as string, cascade delete of line items |
| `Persistence/AppDbContextFactory.cs` | Design-time factory so `dotnet ef` can build the context for migrations |
| `Persistence/DbInitializer.cs` | Applies pending migrations on startup and seeds demo data when the database is empty |
| `Migrations/` | Generated EF Core migrations |
| `DependencyInjection.cs` | `AddInfrastructure`: registers the context with Npgsql and binds IAppDbContext to it |
| `Telemetry/TelemetryExtensions.cs` | `AddSeeSharpTelemetry`: traces, metrics, and logs over OTLP |
| `Telemetry/AppTelemetry.cs` | The shared ActivitySource for hand-rolled spans |
| `Telemetry/AppMetrics.cs` | The custom meter with the `invoices_created` counter |

### SeeSharp.Api

The HTTP shell. Thin by design: routing, auth, validation calls, error
mapping. All decisions happen further in.

| File | Responsibility |
| --- | --- |
| `Program.cs` | Composition root: DI wiring, middleware order, endpoint mapping, database initialization |
| `Auth/TokenAuthMiddleware.cs` | Single-account bearer token check on every route except swagger, health, and openapi |
| `ExceptionHandling/DomainExceptionHandler.cs` | Maps exceptions to RFC 9457 ProblemDetails: 400 for validation and argument errors, 409 for illegal invoice transitions, 500 otherwise |
| `Endpoints/ValidationExtensions.cs` | `ValidateAndThrowAsync` helper for FluentValidation |
| `Endpoints/ClientsEndpoints.cs` | `/clients` routes |
| `Endpoints/CategoriesEndpoints.cs` | `/categories` routes |
| `Endpoints/ExpensesEndpoints.cs` | `/expenses` routes |
| `Endpoints/InvoicesEndpoints.cs` | `/invoices` routes, including `POST /invoices/{id}/status` |
| `Endpoints/ReportsEndpoints.cs` | `/reports/summary` route |

### Test projects

| Project | Coverage |
| --- | --- |
| `SeeSharp.Domain.Tests` | Money, Expense, and the Invoice lifecycle rules, all in memory |
| `SeeSharp.Api.Tests` | HTTP integration tests with Testcontainers: a throwaway Postgres per fixture, testing auth, client CRUD, and the 409 on an illegal transition |

## Data model

```
Client 1 ----< Invoice 1 ----< InvoiceLineItem
Category 1 ----< Expense
```

| Entity | Table | Notes |
| --- | --- | --- |
| Client | `clients` | name required, email and address optional |
| Invoice | `invoices` | status stored as string, indexes on status and client |
| InvoiceLineItem | `invoice_line_items` | cascade-deleted with the invoice |
| Expense | `expenses` | index on date |
| Category | `categories` | name required |

Money is always `decimal` in C# and `numeric(18,2)` in Postgres. Never a
float. Computed values (`Total`, `LineTotal`) are not stored; they are derived
from line items.

## Request lifecycle: POST /invoices

1. **Routing.** ASP.NET matches the route registered in `InvoicesEndpoints`.
   An HTTP span starts from the ASP.NET Core auto-instrumentation.
2. **Auth.** `TokenAuthMiddleware` compares the bearer token to `Auth:Token`.
   A mismatch short-circuits with 401 before any handler runs.
3. **Validation.** The endpoint resolves `IValidator<CreateInvoiceRequest>`
   and calls `ValidateAndThrowAsync`. Failures throw `ValidationException`,
   which `DomainExceptionHandler` turns into a 400 ProblemDetails.
4. **Handler.** `InvoiceHandlers.CreateAsync` asks the domain for a new
   invoice: `Invoice.Create(...)` enforces its invariants, then each line item
   goes through `AddLineItem`, which only works while the invoice is a draft.
   Any rule broken here throws a domain exception and becomes a 400 or 409.
5. **Persistence.** The context inserts the invoice and its line items in one
   SaveChanges. The EF Core instrumentation adds child spans for the SQL.
6. **Mapping.** The entity becomes an `InvoiceResponse` record. Entities never
   cross the HTTP boundary.
7. **Telemetry.** `metrics.InvoiceCreated()` increments the counter. The whole
   trace, HTTP span plus EF child spans, exports to SigNoz over OTLP.

Status changes (`POST /invoices/{id}/status`) follow the same path, plus one
hand-rolled span named `invoice.status_change` that wraps the call, so the
manual tracing path is demonstrated next to the auto-instrumentation.

## Telemetry flow

`AddSeeSharpTelemetry` in `TelemetryExtensions.cs` wires everything at startup:

- **Traces:** ASP.NET Core, HttpClient, and EF Core instrumentation, plus the
  app `ActivitySource` for manual spans.
- **Metrics:** ASP.NET Core, HttpClient, and .NET runtime instrumentation,
  plus the custom `SeeSharp.Api` meter.
- **Logs:** the OpenTelemetry logger provider with scopes included.

Everything exports over OTLP gRPC to `Otel:Endpoint` (default
`http://localhost:4317`), where the SigNoz ingester listens. The service name
is `see-sharp-api`. Export failures never block requests: if SigNoz is down
the API keeps working and only the exporter logs warnings.

## Configuration

| Setting | Where it is read | Purpose |
| --- | --- | --- |
| `ConnectionStrings:AppDb` | `Program.cs` | Npgsql connection string |
| `Auth:Token` | `TokenAuthMiddleware` | The single-account bearer token |
| `Otel:Endpoint` | `TelemetryExtensions` | OTLP collector address |
| `Cors:AllowedOrigins` | `Program.cs` | Origins the SPA may call from |

Base values live in `api/SeeSharp.Api/appsettings.json`, development overrides
in `appsettings.Development.json`, and every value can be overridden with
environment variables (`ConnectionStrings__AppDb`, `Auth__Token`, and so on).
The compose file sets them that way for the containerized run.

## Frontend map

React Router routes in `web/src/main.tsx` mount four pages under the shared
nav layout in `App.tsx`:

| Route | Page | API calls |
| --- | --- | --- |
| `/` | `pages/Dashboard.tsx` | `GET /reports/summary` for the current year |
| `/clients` | `pages/Clients.tsx` | `GET`/`POST /clients` |
| `/invoices` | `pages/Invoices.tsx` | `GET /invoices` |
| `/expenses` | `pages/Expenses.tsx` | `GET /expenses` |

All pages go through the typed client in `web/src/api/client.ts`, which
attaches the bearer token and unwraps ProblemDetails into an `ApiError`. Types
in `web/src/api/types.ts` mirror the API DTOs. In development, Vite proxies
`/api` to `http://localhost:5080` (see `web/vite.config.ts`), so the browser
never deals with CORS.

## Ports

| Port | What listens |
| --- | --- |
| 5080 | The API (host port; the container serves 8080 internally) |
| 5173 | Vite dev server |
| 8081 | The containerized web app |
| 5432 | PostgreSQL |
| 8080 | SigNoz UI |
| 4317 / 4318 | SigNoz OTLP ingester (gRPC / HTTP) |
