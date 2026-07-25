# See Sharp Invoicing API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a working end-to-end freelancer invoicing and expense tracker: a Clean-ish C# REST API on .NET 10 with PostgreSQL, full OpenTelemetry to SigNoz, and a React frontend, all runnable locally with Docker for the dependencies.

**Architecture:** Four C# projects with dependencies pointing inward (Domain <- Application <- Infrastructure <- Api). The domain owns the business rules and has no dependencies. EF Core with Npgsql provides persistence via a single `IAppDbContext` interface. Minimal API endpoints stay thin and call plain use-case handlers. A React (Vite + TS) SPA consumes the API over HTTP.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, EF Core 9/10 + Npgsql, FluentValidation, xUnit, Testcontainers, OpenTelemetry .NET SDK, PostgreSQL, SigNoz, React + Vite + TypeScript + React Router, Docker Compose.

## Global Constraints

- .NET 10 SDK, installed via Homebrew (`brew install --cask dotnet-sdk`).
- C# app projects (Domain, Application, Infrastructure, Api): nullable reference types on, warnings as errors, file-scoped namespaces.
- Money is `decimal` in C# and `numeric(18,2)` in Postgres. Never `double`.
- EF entities are never exposed over HTTP. DTOs are `record` types.
- No MediatR, no AutoMapper, no repository interfaces. Application depends on one `IAppDbContext` interface only.
- Domain project references nothing (no EF Core, no ASP.NET, no NuGet beyond the framework).
- All error responses are RFC 9457 `ProblemDetails`.
- Every request must present the hardcoded auth token (single account).
- OpenTelemetry service name is `see-sharp-api`. Telemetry export must never block or crash the app when SigNoz is down.
- All docs are written in a plain human voice: no em dashes, no AI filler.
- Use the machine's existing git config. Do not pass `-c user.name` or `-c user.email`.
- Conventional commit messages, kept short.

## Solution and Namespace Conventions

- Solution file: `api/SeeSharp.sln`.
- Namespaces: `SeeSharp.Domain`, `SeeSharp.Application`, `SeeSharp.Infrastructure`, `SeeSharp.Api`, and matching test namespaces.
- Root namespace per project equals the project name.
- Ports: API `5080`, Vite dev `5173`, Postgres `5432`, SigNoz UI `3301`.

---

## Phase 0: Tooling and Solution Scaffold

### Task 0.1: Install .NET 10 SDK and Node via Homebrew

**Files:** none (environment setup).

- [ ] **Step 1: Install the .NET SDK**

Run:
```bash
brew install --cask dotnet-sdk
```
Expected: installs .NET SDK 10.x. If already installed, brew reports it is up to date.

- [ ] **Step 2: Install Node**

Run:
```bash
brew install node
```
Expected: installs a current Node LTS with npm.

- [ ] **Step 3: Verify both toolchains**

Run:
```bash
dotnet --version && node --version && npm --version
```
Expected: `dotnet --version` prints `10.x.x`; `node --version` prints `v20+` or newer; npm prints a version.

If `dotnet` is not found after install, the cask may put it at `/usr/local/share/dotnet` (Intel) or `/opt/homebrew/...`; run `brew info --cask dotnet-sdk` and add the printed path to `PATH`, then re-run the verify step.

### Task 0.2: Scaffold the solution and four projects

**Files:**
- Create: `api/SeeSharp.sln`
- Create: `api/SeeSharp.Domain/SeeSharp.Domain.csproj`
- Create: `api/SeeSharp.Application/SeeSharp.Application.csproj`
- Create: `api/SeeSharp.Infrastructure/SeeSharp.Infrastructure.csproj`
- Create: `api/SeeSharp.Api/SeeSharp.Api.csproj`
- Create: `api/Directory.Build.props`

**Interfaces:**
- Produces: the four projects and their reference graph (Api -> Infrastructure -> Application -> Domain), consumed by every later task.

- [ ] **Step 1: Create the solution and class libraries**

Run from `api/`:
```bash
cd api
dotnet new sln -n SeeSharp
dotnet new classlib -n SeeSharp.Domain -f net10.0
dotnet new classlib -n SeeSharp.Application -f net10.0
dotnet new classlib -n SeeSharp.Infrastructure -f net10.0
dotnet new web -n SeeSharp.Api -f net10.0
```

- [ ] **Step 2: Remove the default `Class1.cs` files**

Delete `api/SeeSharp.Domain/Class1.cs`, `api/SeeSharp.Application/Class1.cs`, and `api/SeeSharp.Infrastructure/Class1.cs`.

- [ ] **Step 3: Wire project references (inward only)**

Run from `api/`:
```bash
dotnet add SeeSharp.Application/SeeSharp.Application.csproj reference SeeSharp.Domain/SeeSharp.Domain.csproj
dotnet add SeeSharp.Infrastructure/SeeSharp.Infrastructure.csproj reference SeeSharp.Application/SeeSharp.Application.csproj
dotnet add SeeSharp.Api/SeeSharp.Api.csproj reference SeeSharp.Infrastructure/SeeSharp.Infrastructure.csproj
```

- [ ] **Step 4: Add all projects to the solution**

Run from `api/`:
```bash
dotnet sln add SeeSharp.Domain SeeSharp.Application SeeSharp.Infrastructure SeeSharp.Api
```

- [ ] **Step 5: Add shared build properties**

Create `api/Directory.Build.props`:
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

Then remove the now-duplicated `<TargetFramework>`, `<Nullable>`, and `<ImplicitUsings>` lines from each `.csproj` so the shared props are the single source of truth.

- [ ] **Step 6: Build the empty solution**

Run from `api/`:
```bash
dotnet build
```
Expected: build succeeds with no warnings.

- [ ] **Step 7: Commit**

```bash
git add api/ .gitignore
git commit -m "chore: scaffold clean-ish solution with four projects"
```

### Task 0.3: Add the test projects

**Files:**
- Create: `api/SeeSharp.Domain.Tests/SeeSharp.Domain.Tests.csproj`
- Create: `api/SeeSharp.Api.Tests/SeeSharp.Api.Tests.csproj`

**Interfaces:**
- Produces: two xUnit test projects referencing Domain and Api respectively.

- [ ] **Step 1: Create the test projects**

Run from `api/`:
```bash
dotnet new xunit -n SeeSharp.Domain.Tests -f net10.0
dotnet new xunit -n SeeSharp.Api.Tests -f net10.0
dotnet add SeeSharp.Domain.Tests reference SeeSharp.Domain
dotnet add SeeSharp.Api.Tests reference SeeSharp.Api
dotnet sln add SeeSharp.Domain.Tests SeeSharp.Api.Tests
```

- [ ] **Step 2: Delete the default `UnitTest1.cs` files**

Delete `api/SeeSharp.Domain.Tests/UnitTest1.cs` and `api/SeeSharp.Api.Tests/UnitTest1.cs`.

- [ ] **Step 3: Run the test suite (empty)**

Run from `api/`:
```bash
dotnet test
```
Expected: build succeeds, 0 tests run, no failures.

- [ ] **Step 4: Commit**

```bash
git add api/
git commit -m "chore: add xunit test projects"
```

---

## Phase 1: Domain

The domain is pure C# with no dependencies. This is where the business rules live and where they are protected. Every task here is test-first.

### Task 1.1: Money value object

**Files:**
- Create: `api/SeeSharp.Domain/ValueObjects/Money.cs`
- Test: `api/SeeSharp.Domain.Tests/ValueObjects/MoneyTests.cs`

**Interfaces:**
- Produces: `readonly record struct Money(decimal Amount)` with a validating factory `Money.From(decimal)`, operators `+`, and a `Zero` static. Amount is always >= 0.

- [ ] **Step 1: Write the failing tests**

Create `api/SeeSharp.Domain.Tests/ValueObjects/MoneyTests.cs`:
```csharp
using SeeSharp.Domain.ValueObjects;

namespace SeeSharp.Domain.Tests.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void From_WithPositiveAmount_StoresAmount()
    {
        var money = Money.From(10.50m);
        Assert.Equal(10.50m, money.Amount);
    }

    [Fact]
    public void From_WithNegativeAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.From(-1m));
    }

    [Fact]
    public void Add_SumsAmounts()
    {
        var result = Money.From(2m) + Money.From(3m);
        Assert.Equal(5m, result.Amount);
    }

    [Fact]
    public void Zero_IsZeroAmount()
    {
        Assert.Equal(0m, Money.Zero.Amount);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test --filter FullyQualifiedName~MoneyTests`
Expected: FAIL, `Money` does not exist.

- [ ] **Step 3: Implement `Money`**

Create `api/SeeSharp.Domain/ValueObjects/Money.cs`:
```csharp
namespace SeeSharp.Domain.ValueObjects;

public readonly record struct Money
{
    public decimal Amount { get; }

    private Money(decimal amount) => Amount = amount;

    public static Money Zero => new(0m);

    public static Money From(decimal amount)
    {
        if (amount < 0m)
            throw new ArgumentOutOfRangeException(nameof(amount), "Money cannot be negative.");
        return new Money(amount);
    }

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test --filter FullyQualifiedName~MoneyTests`
Expected: PASS, 4 tests.

- [ ] **Step 5: Commit**

```bash
git add api/SeeSharp.Domain api/SeeSharp.Domain.Tests
git commit -m "feat(domain): add Money value object"
```

### Task 1.2: InvoiceStatus enum and transition exception

**Files:**
- Create: `api/SeeSharp.Domain/Enums/InvoiceStatus.cs`
- Create: `api/SeeSharp.Domain/Exceptions/InvalidInvoiceTransitionException.cs`

**Interfaces:**
- Produces: `enum InvoiceStatus { Draft, Sent, Paid, Overdue, Cancelled }` and `InvalidInvoiceTransitionException(InvoiceStatus from, InvoiceStatus to)` extending `InvalidOperationException`. Consumed by the Invoice entity in Task 1.4.

- [ ] **Step 1: Create the enum**

Create `api/SeeSharp.Domain/Enums/InvoiceStatus.cs`:
```csharp
namespace SeeSharp.Domain.Enums;

public enum InvoiceStatus
{
    Draft,
    Sent,
    Paid,
    Overdue,
    Cancelled
}
```

- [ ] **Step 2: Create the exception**

Create `api/SeeSharp.Domain/Exceptions/InvalidInvoiceTransitionException.cs`:
```csharp
using SeeSharp.Domain.Enums;

namespace SeeSharp.Domain.Exceptions;

public sealed class InvalidInvoiceTransitionException(InvoiceStatus from, InvoiceStatus to)
    : InvalidOperationException($"Cannot transition invoice from {from} to {to}.")
{
    public InvoiceStatus From { get; } = from;
    public InvoiceStatus To { get; } = to;
}
```

- [ ] **Step 3: Build**

Run: `dotnet build api/SeeSharp.Domain`
Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add api/SeeSharp.Domain
git commit -m "feat(domain): add invoice status enum and transition exception"
```

### Task 1.3: Client, Category, and Expense entities

**Files:**
- Create: `api/SeeSharp.Domain/Entities/Client.cs`
- Create: `api/SeeSharp.Domain/Entities/Category.cs`
- Create: `api/SeeSharp.Domain/Entities/Expense.cs`
- Test: `api/SeeSharp.Domain.Tests/Entities/ExpenseTests.cs`

**Interfaces:**
- Produces:
  - `Client` with `Guid Id`, `string Name`, `string? Email`, `string? Address`, `DateTimeOffset CreatedAt`, factory `Client.Create(name, email, address)`.
  - `Category` with `Guid Id`, `string Name`, factory `Category.Create(name)`.
  - `Expense` with `Guid Id`, `Guid? CategoryId`, `string Description`, `decimal Amount`, `DateOnly Date`, `string? Vendor`, `DateTimeOffset CreatedAt`, factory `Expense.Create(description, amount, date, vendor, categoryId)`; `Amount` validated via `Money`.

- [ ] **Step 1: Write the failing Expense tests**

Create `api/SeeSharp.Domain.Tests/Entities/ExpenseTests.cs`:
```csharp
using SeeSharp.Domain.Entities;

namespace SeeSharp.Domain.Tests.Entities;

public class ExpenseTests
{
    [Fact]
    public void Create_SetsFieldsAndGeneratesId()
    {
        var date = new DateOnly(2026, 7, 25);
        var expense = Expense.Create("Domain renewal", 12.00m, date, "Namecheap", null);

        Assert.NotEqual(Guid.Empty, expense.Id);
        Assert.Equal("Domain renewal", expense.Description);
        Assert.Equal(12.00m, expense.Amount);
        Assert.Equal(date, expense.Date);
        Assert.Equal("Namecheap", expense.Vendor);
        Assert.Null(expense.CategoryId);
    }

    [Fact]
    public void Create_WithNegativeAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Expense.Create("Bad", -5m, new DateOnly(2026, 1, 1), null, null));
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test --filter FullyQualifiedName~ExpenseTests`
Expected: FAIL, `Expense` does not exist.

- [ ] **Step 3: Implement Client**

Create `api/SeeSharp.Domain/Entities/Client.cs`:
```csharp
namespace SeeSharp.Domain.Entities;

public sealed class Client
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Client() { }

    public static Client Create(string name, string? email, string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Client name is required.", nameof(name));

        return new Client
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Email = email?.Trim(),
            Address = address?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string? email, string? address)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Client name is required.", nameof(name));
        Name = name.Trim();
        Email = email?.Trim();
        Address = address?.Trim();
    }
}
```

- [ ] **Step 4: Implement Category**

Create `api/SeeSharp.Domain/Entities/Category.cs`:
```csharp
namespace SeeSharp.Domain.Entities;

public sealed class Category
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;

    private Category() { }

    public static Category Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));
        return new Category { Id = Guid.NewGuid(), Name = name.Trim() };
    }
}
```

- [ ] **Step 5: Implement Expense**

Create `api/SeeSharp.Domain/Entities/Expense.cs`:
```csharp
using SeeSharp.Domain.ValueObjects;

namespace SeeSharp.Domain.Entities;

public sealed class Expense
{
    public Guid Id { get; private set; }
    public Guid? CategoryId { get; private set; }
    public string Description { get; private set; } = default!;
    public decimal Amount { get; private set; }
    public DateOnly Date { get; private set; }
    public string? Vendor { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Expense() { }

    public static Expense Create(string description, decimal amount, DateOnly date, string? vendor, Guid? categoryId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Expense description is required.", nameof(description));

        return new Expense
        {
            Id = Guid.NewGuid(),
            Description = description.Trim(),
            Amount = Money.From(amount).Amount,
            Date = date,
            Vendor = vendor?.Trim(),
            CategoryId = categoryId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(string description, decimal amount, DateOnly date, string? vendor, Guid? categoryId)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Expense description is required.", nameof(description));
        Description = description.Trim();
        Amount = Money.From(amount).Amount;
        Date = date;
        Vendor = vendor?.Trim();
        CategoryId = categoryId;
    }
}
```

- [ ] **Step 6: Run to verify pass**

Run: `dotnet test --filter FullyQualifiedName~ExpenseTests`
Expected: PASS, 2 tests.

- [ ] **Step 7: Commit**

```bash
git add api/SeeSharp.Domain api/SeeSharp.Domain.Tests
git commit -m "feat(domain): add Client, Category, and Expense entities"
```

### Task 1.4: Invoice and InvoiceLineItem entities with status rules

**Files:**
- Create: `api/SeeSharp.Domain/Entities/InvoiceLineItem.cs`
- Create: `api/SeeSharp.Domain/Entities/Invoice.cs`
- Test: `api/SeeSharp.Domain.Tests/Entities/InvoiceTests.cs`

**Interfaces:**
- Produces:
  - `InvoiceLineItem` with `Guid Id`, `Guid InvoiceId`, `string Description`, `int Quantity`, `decimal UnitPrice`, computed `decimal LineTotal => Quantity * UnitPrice`.
  - `Invoice` with `Guid Id`, `Guid ClientId`, `string Number`, `InvoiceStatus Status` (private setter), `DateOnly IssueDate`, `DateOnly DueDate`, `string? Notes`, `DateTimeOffset CreatedAt`, read-only `IReadOnlyCollection<InvoiceLineItem> LineItems`, computed `decimal Total`. Factory `Invoice.Create(clientId, number, issueDate, dueDate, notes)` starts in `Draft`. Methods `AddLineItem(description, quantity, unitPrice)`, `MarkAsSent()`, `MarkAsPaid()`, `MarkAsOverdue()`, `Cancel()`. Transitions enforce the rules below and throw `InvalidInvoiceTransitionException` when illegal.
- Transition rules:
  - `MarkAsSent`: allowed only from `Draft`.
  - `MarkAsPaid`: allowed from `Sent` or `Overdue`.
  - `MarkAsOverdue`: allowed only from `Sent`.
  - `Cancel`: allowed from `Draft` or `Sent`.
  - `AddLineItem`: allowed only while `Draft`.

- [ ] **Step 1: Write the failing Invoice tests**

Create `api/SeeSharp.Domain.Tests/Entities/InvoiceTests.cs`:
```csharp
using SeeSharp.Domain.Entities;
using SeeSharp.Domain.Enums;
using SeeSharp.Domain.Exceptions;

namespace SeeSharp.Domain.Tests.Entities;

public class InvoiceTests
{
    private static Invoice NewDraft()
    {
        var invoice = Invoice.Create(
            Guid.NewGuid(), "INV-001",
            new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31), null);
        return invoice;
    }

    [Fact]
    public void Create_StartsInDraft()
    {
        Assert.Equal(InvoiceStatus.Draft, NewDraft().Status);
    }

    [Fact]
    public void Total_SumsLineItems()
    {
        var invoice = NewDraft();
        invoice.AddLineItem("Design", 2, 100m);
        invoice.AddLineItem("Hosting", 1, 25m);
        Assert.Equal(225m, invoice.Total);
    }

    [Fact]
    public void MarkAsPaid_FromDraft_Throws()
    {
        var invoice = NewDraft();
        Assert.Throws<InvalidInvoiceTransitionException>(() => invoice.MarkAsPaid());
    }

    [Fact]
    public void MarkAsPaid_FromSent_Succeeds()
    {
        var invoice = NewDraft();
        invoice.MarkAsSent();
        invoice.MarkAsPaid();
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void AddLineItem_AfterSent_Throws()
    {
        var invoice = NewDraft();
        invoice.MarkAsSent();
        Assert.Throws<InvalidOperationException>(() => invoice.AddLineItem("Late", 1, 10m));
    }

    [Fact]
    public void MarkAsSent_Twice_Throws()
    {
        var invoice = NewDraft();
        invoice.MarkAsSent();
        Assert.Throws<InvalidInvoiceTransitionException>(() => invoice.MarkAsSent());
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test --filter FullyQualifiedName~InvoiceTests`
Expected: FAIL, `Invoice` does not exist.

- [ ] **Step 3: Implement InvoiceLineItem**

Create `api/SeeSharp.Domain/Entities/InvoiceLineItem.cs`:
```csharp
namespace SeeSharp.Domain.Entities;

public sealed class InvoiceLineItem
{
    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; } = default!;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    private InvoiceLineItem() { }

    internal static InvoiceLineItem Create(Guid invoiceId, string description, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Line item description is required.", nameof(description));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (unitPrice < 0m)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");

        return new InvoiceLineItem
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            Description = description.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}
```

- [ ] **Step 4: Implement Invoice**

Create `api/SeeSharp.Domain/Entities/Invoice.cs`:
```csharp
using SeeSharp.Domain.Enums;
using SeeSharp.Domain.Exceptions;

namespace SeeSharp.Domain.Entities;

public sealed class Invoice
{
    private readonly List<InvoiceLineItem> _lineItems = [];

    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public string Number { get; private set; } = default!;
    public InvoiceStatus Status { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();
    public decimal Total => _lineItems.Sum(item => item.LineTotal);

    private Invoice() { }

    public static Invoice Create(Guid clientId, string number, DateOnly issueDate, DateOnly dueDate, string? notes)
    {
        if (clientId == Guid.Empty)
            throw new ArgumentException("ClientId is required.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Invoice number is required.", nameof(number));
        if (dueDate < issueDate)
            throw new ArgumentException("Due date cannot be before issue date.", nameof(dueDate));

        return new Invoice
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Number = number.Trim(),
            Status = InvoiceStatus.Draft,
            IssueDate = issueDate,
            DueDate = dueDate,
            Notes = notes?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void AddLineItem(string description, int quantity, decimal unitPrice)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Line items can only be added while the invoice is a draft.");
        _lineItems.Add(InvoiceLineItem.Create(Id, description, quantity, unitPrice));
    }

    public void ClearLineItems()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Line items can only be changed while the invoice is a draft.");
        _lineItems.Clear();
    }

    public void MarkAsSent()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidInvoiceTransitionException(Status, InvoiceStatus.Sent);
        Status = InvoiceStatus.Sent;
    }

    public void MarkAsPaid()
    {
        if (Status is not (InvoiceStatus.Sent or InvoiceStatus.Overdue))
            throw new InvalidInvoiceTransitionException(Status, InvoiceStatus.Paid);
        Status = InvoiceStatus.Paid;
    }

    public void MarkAsOverdue()
    {
        if (Status != InvoiceStatus.Sent)
            throw new InvalidInvoiceTransitionException(Status, InvoiceStatus.Overdue);
        Status = InvoiceStatus.Overdue;
    }

    public void Cancel()
    {
        if (Status is not (InvoiceStatus.Draft or InvoiceStatus.Sent))
            throw new InvalidInvoiceTransitionException(Status, InvoiceStatus.Cancelled);
        Status = InvoiceStatus.Cancelled;
    }
}
```

- [ ] **Step 5: Run to verify pass**

Run: `dotnet test --filter FullyQualifiedName~InvoiceTests`
Expected: PASS, 6 tests.

- [ ] **Step 6: Run the full domain suite**

Run: `dotnet test api/SeeSharp.Domain.Tests`
Expected: all domain tests pass.

- [ ] **Step 7: Commit**

```bash
git add api/SeeSharp.Domain api/SeeSharp.Domain.Tests
git commit -m "feat(domain): add Invoice and line items with status rules"
```

---

## Phase 2: Application

DTOs, validators, the `IAppDbContext` interface, and use-case handlers. Handlers are plain classes. The Application project references FluentValidation and EF Core's abstractions only through `IAppDbContext` (which exposes `DbSet<T>` and `SaveChangesAsync`). To let Application name `DbSet<T>` without a full EF dependency, it references `Microsoft.EntityFrameworkCore` (the core package provides `DbSet`), which is acceptable here and keeps the interface honest.

### Task 2.1: Add packages and the IAppDbContext interface

**Files:**
- Modify: `api/SeeSharp.Application/SeeSharp.Application.csproj`
- Create: `api/SeeSharp.Application/Abstractions/IAppDbContext.cs`
- Create: `api/SeeSharp.Application/Common/PagedResult.cs`

**Interfaces:**
- Produces:
  - `IAppDbContext` exposing `DbSet<Client> Clients`, `DbSet<Invoice> Invoices`, `DbSet<InvoiceLineItem> InvoiceLineItems`, `DbSet<Expense> Expenses`, `DbSet<Category> Categories`, and `Task<int> SaveChangesAsync(CancellationToken)`.
  - `PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)` record with computed `int TotalPages`.

- [ ] **Step 1: Add packages**

Run from `api/`:
```bash
dotnet add SeeSharp.Application package Microsoft.EntityFrameworkCore
dotnet add SeeSharp.Application package FluentValidation
```

- [ ] **Step 2: Create `IAppDbContext`**

Create `api/SeeSharp.Application/Abstractions/IAppDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Client> Clients { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLineItem> InvoiceLineItems { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<Category> Categories { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Create `PagedResult`**

Create `api/SeeSharp.Application/Common/PagedResult.cs`:
```csharp
namespace SeeSharp.Application.Common;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
```

- [ ] **Step 4: Build**

Run: `dotnet build api/SeeSharp.Application`
Expected: succeeds.

- [ ] **Step 5: Commit**

```bash
git add api/SeeSharp.Application
git commit -m "feat(application): add IAppDbContext and PagedResult"
```

### Task 2.2: Client DTOs, validator, and handlers

**Files:**
- Create: `api/SeeSharp.Application/Clients/ClientDtos.cs`
- Create: `api/SeeSharp.Application/Clients/ClientValidators.cs`
- Create: `api/SeeSharp.Application/Clients/ClientHandlers.cs`

**Interfaces:**
- Consumes: `IAppDbContext`, `PagedResult<T>`, `Client`.
- Produces:
  - `record CreateClientRequest(string Name, string? Email, string? Address)`.
  - `record UpdateClientRequest(string Name, string? Email, string? Address)`.
  - `record ClientResponse(Guid Id, string Name, string? Email, string? Address, DateTimeOffset CreatedAt)` with static `ClientResponse.From(Client)`.
  - `CreateClientRequestValidator`, `UpdateClientRequestValidator` (name required, max 200; email valid when present).
  - `ClientHandlers` static class with:
    - `Task<ClientResponse> CreateAsync(IAppDbContext db, CreateClientRequest req, CancellationToken ct)`
    - `Task<ClientResponse?> UpdateAsync(IAppDbContext db, Guid id, UpdateClientRequest req, CancellationToken ct)`
    - `Task<bool> DeleteAsync(IAppDbContext db, Guid id, CancellationToken ct)`
    - `Task<ClientResponse?> GetAsync(IAppDbContext db, Guid id, CancellationToken ct)`
    - `Task<PagedResult<ClientResponse>> ListAsync(IAppDbContext db, int page, int pageSize, CancellationToken ct)`

- [ ] **Step 1: Create the DTOs**

Create `api/SeeSharp.Application/Clients/ClientDtos.cs`:
```csharp
using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Clients;

public record CreateClientRequest(string Name, string? Email, string? Address);

public record UpdateClientRequest(string Name, string? Email, string? Address);

public record ClientResponse(Guid Id, string Name, string? Email, string? Address, DateTimeOffset CreatedAt)
{
    public static ClientResponse From(Client client) =>
        new(client.Id, client.Name, client.Email, client.Address, client.CreatedAt);
}
```

- [ ] **Step 2: Create the validators**

Create `api/SeeSharp.Application/Clients/ClientValidators.cs`:
```csharp
using FluentValidation;

namespace SeeSharp.Application.Clients;

public sealed class CreateClientRequestValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public sealed class UpdateClientRequestValidator : AbstractValidator<UpdateClientRequest>
{
    public UpdateClientRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}
```

- [ ] **Step 3: Create the handlers**

Create `api/SeeSharp.Application/Clients/ClientHandlers.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Common;
using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Clients;

public static class ClientHandlers
{
    public static async Task<ClientResponse> CreateAsync(
        IAppDbContext db, CreateClientRequest req, CancellationToken ct)
    {
        var client = Client.Create(req.Name, req.Email, req.Address);
        db.Clients.Add(client);
        await db.SaveChangesAsync(ct);
        return ClientResponse.From(client);
    }

    public static async Task<ClientResponse?> UpdateAsync(
        IAppDbContext db, Guid id, UpdateClientRequest req, CancellationToken ct)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (client is null) return null;
        client.Update(req.Name, req.Email, req.Address);
        await db.SaveChangesAsync(ct);
        return ClientResponse.From(client);
    }

    public static async Task<bool> DeleteAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (client is null) return false;
        db.Clients.Remove(client);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public static async Task<ClientResponse?> GetAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var client = await db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        return client is null ? null : ClientResponse.From(client);
    }

    public static async Task<PagedResult<ClientResponse>> ListAsync(
        IAppDbContext db, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Clients.AsNoTracking().OrderBy(c => c.Name);
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(c => new ClientResponse(c.Id, c.Name, c.Email, c.Address, c.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ClientResponse>(items, page, pageSize, total);
    }
}
```

- [ ] **Step 4: Build**

Run: `dotnet build api/SeeSharp.Application`
Expected: succeeds.

- [ ] **Step 5: Commit**

```bash
git add api/SeeSharp.Application
git commit -m "feat(application): add client dtos, validators, and handlers"
```

### Task 2.3: Category and Expense DTOs, validators, and handlers

**Files:**
- Create: `api/SeeSharp.Application/Categories/CategoryDtos.cs`
- Create: `api/SeeSharp.Application/Categories/CategoryHandlers.cs`
- Create: `api/SeeSharp.Application/Expenses/ExpenseDtos.cs`
- Create: `api/SeeSharp.Application/Expenses/ExpenseValidators.cs`
- Create: `api/SeeSharp.Application/Expenses/ExpenseHandlers.cs`

**Interfaces:**
- Consumes: `IAppDbContext`, `PagedResult<T>`, `Category`, `Expense`.
- Produces:
  - `record CreateCategoryRequest(string Name)`; `record CategoryResponse(Guid Id, string Name)` with `From(Category)`.
  - `CategoryHandlers` static: `CreateAsync`, `ListAsync`, `DeleteAsync` (signatures mirror client handlers, no update).
  - `record CreateExpenseRequest(string Description, decimal Amount, DateOnly Date, string? Vendor, Guid? CategoryId)`; `UpdateExpenseRequest` with the same shape; `record ExpenseResponse(Guid Id, Guid? CategoryId, string Description, decimal Amount, DateOnly Date, string? Vendor, DateTimeOffset CreatedAt)` with `From(Expense)`.
  - `CreateExpenseRequestValidator`, `UpdateExpenseRequestValidator` (description required max 500; amount >= 0).
  - `ExpenseHandlers` static: `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `GetAsync`, and `ListAsync(IAppDbContext db, Guid? categoryId, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct)`.

- [ ] **Step 1: Create category DTOs**

Create `api/SeeSharp.Application/Categories/CategoryDtos.cs`:
```csharp
using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Categories;

public record CreateCategoryRequest(string Name);

public record CategoryResponse(Guid Id, string Name)
{
    public static CategoryResponse From(Category category) => new(category.Id, category.Name);
}
```

- [ ] **Step 2: Create category handlers**

Create `api/SeeSharp.Application/Categories/CategoryHandlers.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using SeeSharp.Application.Abstractions;
using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Categories;

public static class CategoryHandlers
{
    public static async Task<CategoryResponse> CreateAsync(
        IAppDbContext db, CreateCategoryRequest req, CancellationToken ct)
    {
        var category = Category.Create(req.Name);
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return CategoryResponse.From(category);
    }

    public static async Task<IReadOnlyList<CategoryResponse>> ListAsync(IAppDbContext db, CancellationToken ct)
    {
        return await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse(c.Id, c.Name))
            .ToListAsync(ct);
    }

    public static async Task<bool> DeleteAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null) return false;
        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
```

- [ ] **Step 3: Create expense DTOs**

Create `api/SeeSharp.Application/Expenses/ExpenseDtos.cs`:
```csharp
using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Expenses;

public record CreateExpenseRequest(string Description, decimal Amount, DateOnly Date, string? Vendor, Guid? CategoryId);

public record UpdateExpenseRequest(string Description, decimal Amount, DateOnly Date, string? Vendor, Guid? CategoryId);

public record ExpenseResponse(
    Guid Id, Guid? CategoryId, string Description, decimal Amount, DateOnly Date, string? Vendor, DateTimeOffset CreatedAt)
{
    public static ExpenseResponse From(Expense expense) =>
        new(expense.Id, expense.CategoryId, expense.Description, expense.Amount, expense.Date, expense.Vendor, expense.CreatedAt);
}
```

- [ ] **Step 4: Create expense validators**

Create `api/SeeSharp.Application/Expenses/ExpenseValidators.cs`:
```csharp
using FluentValidation;

namespace SeeSharp.Application.Expenses;

public sealed class CreateExpenseRequestValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0m);
    }
}

public sealed class UpdateExpenseRequestValidator : AbstractValidator<UpdateExpenseRequest>
{
    public UpdateExpenseRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0m);
    }
}
```

- [ ] **Step 5: Create expense handlers**

Create `api/SeeSharp.Application/Expenses/ExpenseHandlers.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Common;
using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Expenses;

public static class ExpenseHandlers
{
    public static async Task<ExpenseResponse> CreateAsync(
        IAppDbContext db, CreateExpenseRequest req, CancellationToken ct)
    {
        var expense = Expense.Create(req.Description, req.Amount, req.Date, req.Vendor, req.CategoryId);
        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);
        return ExpenseResponse.From(expense);
    }

    public static async Task<ExpenseResponse?> UpdateAsync(
        IAppDbContext db, Guid id, UpdateExpenseRequest req, CancellationToken ct)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (expense is null) return null;
        expense.Update(req.Description, req.Amount, req.Date, req.Vendor, req.CategoryId);
        await db.SaveChangesAsync(ct);
        return ExpenseResponse.From(expense);
    }

    public static async Task<bool> DeleteAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (expense is null) return false;
        db.Expenses.Remove(expense);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public static async Task<ExpenseResponse?> GetAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var expense = await db.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        return expense is null ? null : ExpenseResponse.From(expense);
    }

    public static async Task<PagedResult<ExpenseResponse>> ListAsync(
        IAppDbContext db, Guid? categoryId, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Expenses.AsNoTracking().AsQueryable();
        if (categoryId is not null) query = query.Where(e => e.CategoryId == categoryId);
        if (from is not null) query = query.Where(e => e.Date >= from);
        if (to is not null) query = query.Where(e => e.Date <= to);

        query = query.OrderByDescending(e => e.Date);
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(e => new ExpenseResponse(e.Id, e.CategoryId, e.Description, e.Amount, e.Date, e.Vendor, e.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<ExpenseResponse>(items, page, pageSize, total);
    }
}
```

- [ ] **Step 6: Build**

Run: `dotnet build api/SeeSharp.Application`
Expected: succeeds.

- [ ] **Step 7: Commit**

```bash
git add api/SeeSharp.Application
git commit -m "feat(application): add category and expense dtos, validators, handlers"
```

### Task 2.4: Invoice DTOs, validator, and handlers

**Files:**
- Create: `api/SeeSharp.Application/Invoices/InvoiceDtos.cs`
- Create: `api/SeeSharp.Application/Invoices/InvoiceValidators.cs`
- Create: `api/SeeSharp.Application/Invoices/InvoiceHandlers.cs`

**Interfaces:**
- Consumes: `IAppDbContext`, `PagedResult<T>`, `Invoice`, `InvoiceStatus`, `InvalidInvoiceTransitionException`.
- Produces:
  - `record LineItemRequest(string Description, int Quantity, decimal UnitPrice)`.
  - `record LineItemResponse(Guid Id, string Description, int Quantity, decimal UnitPrice, decimal LineTotal)`.
  - `record CreateInvoiceRequest(Guid ClientId, string Number, DateOnly IssueDate, DateOnly DueDate, string? Notes, IReadOnlyList<LineItemRequest> LineItems)`.
  - `record UpdateInvoiceRequest(string Number, DateOnly IssueDate, DateOnly DueDate, string? Notes, IReadOnlyList<LineItemRequest> LineItems)` (only allowed while Draft).
  - `record ChangeStatusRequest(string Status)` where Status is one of `sent`, `paid`, `overdue`, `cancelled`.
  - `record InvoiceResponse(Guid Id, Guid ClientId, string Number, string Status, DateOnly IssueDate, DateOnly DueDate, string? Notes, decimal Total, DateTimeOffset CreatedAt, IReadOnlyList<LineItemResponse> LineItems)` with `From(Invoice)`.
  - `CreateInvoiceRequestValidator`, `UpdateInvoiceRequestValidator`.
  - `InvoiceHandlers` static class:
    - `CreateAsync(IAppDbContext, CreateInvoiceRequest, CancellationToken) -> InvoiceResponse`
    - `UpdateAsync(IAppDbContext, Guid, UpdateInvoiceRequest, CancellationToken) -> InvoiceResponse?`
    - `DeleteAsync(IAppDbContext, Guid, CancellationToken) -> bool`
    - `GetAsync(IAppDbContext, Guid, CancellationToken) -> InvoiceResponse?`
    - `ListAsync(IAppDbContext, InvoiceStatus? status, Guid? clientId, int page, int pageSize, CancellationToken) -> PagedResult<InvoiceResponse>`
    - `ChangeStatusAsync(IAppDbContext, Guid, string status, CancellationToken) -> InvoiceResponse?` returning null when not found and throwing `InvalidInvoiceTransitionException` on an illegal transition or `ArgumentException` on an unknown status string.

- [ ] **Step 1: Create the DTOs**

Create `api/SeeSharp.Application/Invoices/InvoiceDtos.cs`:
```csharp
using SeeSharp.Domain.Entities;

namespace SeeSharp.Application.Invoices;

public record LineItemRequest(string Description, int Quantity, decimal UnitPrice);

public record LineItemResponse(Guid Id, string Description, int Quantity, decimal UnitPrice, decimal LineTotal);

public record CreateInvoiceRequest(
    Guid ClientId, string Number, DateOnly IssueDate, DateOnly DueDate, string? Notes,
    IReadOnlyList<LineItemRequest> LineItems);

public record UpdateInvoiceRequest(
    string Number, DateOnly IssueDate, DateOnly DueDate, string? Notes,
    IReadOnlyList<LineItemRequest> LineItems);

public record ChangeStatusRequest(string Status);

public record InvoiceResponse(
    Guid Id, Guid ClientId, string Number, string Status,
    DateOnly IssueDate, DateOnly DueDate, string? Notes, decimal Total, DateTimeOffset CreatedAt,
    IReadOnlyList<LineItemResponse> LineItems)
{
    public static InvoiceResponse From(Invoice invoice) => new(
        invoice.Id, invoice.ClientId, invoice.Number, invoice.Status.ToString(),
        invoice.IssueDate, invoice.DueDate, invoice.Notes, invoice.Total, invoice.CreatedAt,
        invoice.LineItems
            .Select(li => new LineItemResponse(li.Id, li.Description, li.Quantity, li.UnitPrice, li.LineTotal))
            .ToList());
}
```

- [ ] **Step 2: Create the validators**

Create `api/SeeSharp.Application/Invoices/InvoiceValidators.cs`:
```csharp
using FluentValidation;

namespace SeeSharp.Application.Invoices;

public sealed class LineItemRequestValidator : AbstractValidator<LineItemRequest>
{
    public LineItemRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0m);
    }
}

public sealed class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.Number).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.IssueDate);
        RuleForEach(x => x.LineItems).SetValidator(new LineItemRequestValidator());
    }
}

public sealed class UpdateInvoiceRequestValidator : AbstractValidator<UpdateInvoiceRequest>
{
    public UpdateInvoiceRequestValidator()
    {
        RuleFor(x => x.Number).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.IssueDate);
        RuleForEach(x => x.LineItems).SetValidator(new LineItemRequestValidator());
    }
}
```

- [ ] **Step 3: Create the handlers**

Create `api/SeeSharp.Application/Invoices/InvoiceHandlers.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Common;
using SeeSharp.Domain.Entities;
using SeeSharp.Domain.Enums;

namespace SeeSharp.Application.Invoices;

public static class InvoiceHandlers
{
    public static async Task<InvoiceResponse> CreateAsync(
        IAppDbContext db, CreateInvoiceRequest req, CancellationToken ct)
    {
        var invoice = Invoice.Create(req.ClientId, req.Number, req.IssueDate, req.DueDate, req.Notes);
        foreach (var li in req.LineItems)
            invoice.AddLineItem(li.Description, li.Quantity, li.UnitPrice);

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);
        return InvoiceResponse.From(invoice);
    }

    public static async Task<InvoiceResponse?> UpdateAsync(
        IAppDbContext db, Guid id, UpdateInvoiceRequest req, CancellationToken ct)
    {
        var invoice = await db.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null) return null;

        invoice.UpdateDetails(req.Number, req.IssueDate, req.DueDate, req.Notes);
        invoice.ClearLineItems();
        foreach (var li in req.LineItems)
            invoice.AddLineItem(li.Description, li.Quantity, li.UnitPrice);

        await db.SaveChangesAsync(ct);
        return InvoiceResponse.From(invoice);
    }

    public static async Task<bool> DeleteAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null) return false;
        db.Invoices.Remove(invoice);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public static async Task<InvoiceResponse?> GetAsync(IAppDbContext db, Guid id, CancellationToken ct)
    {
        var invoice = await db.Invoices.AsNoTracking()
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        return invoice is null ? null : InvoiceResponse.From(invoice);
    }

    public static async Task<PagedResult<InvoiceResponse>> ListAsync(
        IAppDbContext db, InvoiceStatus? status, Guid? clientId, int page, int pageSize, CancellationToken ct)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = db.Invoices.AsNoTracking().Include(i => i.LineItems).AsQueryable();
        if (status is not null) query = query.Where(i => i.Status == status);
        if (clientId is not null) query = query.Where(i => i.ClientId == clientId);

        query = query.OrderByDescending(i => i.IssueDate);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<InvoiceResponse>(
            items.Select(InvoiceResponse.From).ToList(), page, pageSize, total);
    }

    public static async Task<InvoiceResponse?> ChangeStatusAsync(
        IAppDbContext db, Guid id, string status, CancellationToken ct)
    {
        var invoice = await db.Invoices.Include(i => i.LineItems).FirstOrDefaultAsync(i => i.Id == id, ct);
        if (invoice is null) return null;

        switch (status.Trim().ToLowerInvariant())
        {
            case "sent": invoice.MarkAsSent(); break;
            case "paid": invoice.MarkAsPaid(); break;
            case "overdue": invoice.MarkAsOverdue(); break;
            case "cancelled": invoice.Cancel(); break;
            default: throw new ArgumentException($"Unknown status '{status}'.", nameof(status));
        }

        await db.SaveChangesAsync(ct);
        return InvoiceResponse.From(invoice);
    }
}
```

- [ ] **Step 4: Add `UpdateDetails` to the Invoice entity**

The handler calls `invoice.UpdateDetails(...)`, which does not exist yet. Add it to `api/SeeSharp.Domain/Entities/Invoice.cs` inside the class, after `Cancel()`:
```csharp
    public void UpdateDetails(string number, DateOnly issueDate, DateOnly dueDate, string? notes)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be edited.");
        if (string.IsNullOrWhiteSpace(number))
            throw new ArgumentException("Invoice number is required.", nameof(number));
        if (dueDate < issueDate)
            throw new ArgumentException("Due date cannot be before issue date.", nameof(dueDate));
        Number = number.Trim();
        IssueDate = issueDate;
        DueDate = dueDate;
        Notes = notes?.Trim();
    }
```

- [ ] **Step 5: Add a domain test for `UpdateDetails`**

Add to `api/SeeSharp.Domain.Tests/Entities/InvoiceTests.cs`:
```csharp
    [Fact]
    public void UpdateDetails_AfterSent_Throws()
    {
        var invoice = NewDraft();
        invoice.MarkAsSent();
        Assert.Throws<InvalidOperationException>(
            () => invoice.UpdateDetails("INV-002", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), null));
    }
```

- [ ] **Step 6: Build and test**

Run: `dotnet build api/SeeSharp.Application` then `dotnet test api/SeeSharp.Domain.Tests`
Expected: build succeeds; domain tests pass including the new one.

- [ ] **Step 7: Commit**

```bash
git add api/SeeSharp.Application api/SeeSharp.Domain api/SeeSharp.Domain.Tests
git commit -m "feat(application): add invoice dtos, validators, and handlers"
```

### Task 2.5: Reports handler

**Files:**
- Create: `api/SeeSharp.Application/Reports/ReportDtos.cs`
- Create: `api/SeeSharp.Application/Reports/ReportHandlers.cs`

**Interfaces:**
- Consumes: `IAppDbContext`, `InvoiceStatus`.
- Produces:
  - `record MonthlySummaryRow(int Year, int Month, decimal Income, decimal Expenses, decimal Net)`.
  - `record SummaryResponse(DateOnly From, DateOnly To, decimal TotalIncome, decimal TotalExpenses, decimal Net, IReadOnlyList<MonthlySummaryRow> Months)`.
  - `ReportHandlers.GetSummaryAsync(IAppDbContext db, DateOnly from, DateOnly to, CancellationToken ct) -> SummaryResponse`. Income counts invoices with status Paid, bucketed by IssueDate month. Expenses bucketed by Date month.

- [ ] **Step 1: Create the DTOs**

Create `api/SeeSharp.Application/Reports/ReportDtos.cs`:
```csharp
namespace SeeSharp.Application.Reports;

public record MonthlySummaryRow(int Year, int Month, decimal Income, decimal Expenses, decimal Net);

public record SummaryResponse(
    DateOnly From, DateOnly To, decimal TotalIncome, decimal TotalExpenses, decimal Net,
    IReadOnlyList<MonthlySummaryRow> Months);
```

- [ ] **Step 2: Create the handler**

Create `api/SeeSharp.Application/Reports/ReportHandlers.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using SeeSharp.Application.Abstractions;
using SeeSharp.Domain.Enums;

namespace SeeSharp.Application.Reports;

public static class ReportHandlers
{
    public static async Task<SummaryResponse> GetSummaryAsync(
        IAppDbContext db, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var paidInvoices = await db.Invoices.AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Paid && i.IssueDate >= from && i.IssueDate <= to)
            .Include(i => i.LineItems)
            .ToListAsync(ct);

        var expenses = await db.Expenses.AsNoTracking()
            .Where(e => e.Date >= from && e.Date <= to)
            .ToListAsync(ct);

        var incomeByMonth = paidInvoices
            .GroupBy(i => (i.IssueDate.Year, i.IssueDate.Month))
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Total));

        var expenseByMonth = expenses
            .GroupBy(e => (e.Date.Year, e.Date.Month))
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

        var months = incomeByMonth.Keys.Union(expenseByMonth.Keys)
            .OrderBy(k => k.Item1).ThenBy(k => k.Item2)
            .Select(k =>
            {
                var income = incomeByMonth.GetValueOrDefault(k, 0m);
                var exp = expenseByMonth.GetValueOrDefault(k, 0m);
                return new MonthlySummaryRow(k.Item1, k.Item2, income, exp, income - exp);
            })
            .ToList();

        var totalIncome = months.Sum(m => m.Income);
        var totalExpenses = months.Sum(m => m.Expenses);

        return new SummaryResponse(from, to, totalIncome, totalExpenses, totalIncome - totalExpenses, months);
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build api/SeeSharp.Application`
Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add api/SeeSharp.Application
git commit -m "feat(application): add monthly summary report handler"
```

---

## Phase 3: Infrastructure (EF Core + Postgres)

### Task 3.1: AppDbContext, entity configurations, and DI

**Files:**
- Modify: `api/SeeSharp.Infrastructure/SeeSharp.Infrastructure.csproj`
- Create: `api/SeeSharp.Infrastructure/Persistence/AppDbContext.cs`
- Create: `api/SeeSharp.Infrastructure/Persistence/Configurations.cs`
- Create: `api/SeeSharp.Infrastructure/DependencyInjection.cs`

**Interfaces:**
- Consumes: `IAppDbContext`, all entities.
- Produces:
  - `AppDbContext : DbContext, IAppDbContext` with the five `DbSet`s.
  - `IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)` registering `AppDbContext` with Npgsql and binding `IAppDbContext` to it.

- [ ] **Step 1: Add packages**

Run from `api/`:
```bash
dotnet add SeeSharp.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add SeeSharp.Infrastructure package Microsoft.EntityFrameworkCore.Design
```

- [ ] **Step 2: Create the entity configurations**

Create `api/SeeSharp.Infrastructure/Persistence/Configurations.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SeeSharp.Domain.Entities;

namespace SeeSharp.Infrastructure.Persistence;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(320);
        builder.Property(c => c.Address).HasMaxLength(1000);
    }
}

internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
    }
}

internal sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Description).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Amount).HasColumnType("numeric(18,2)");
        builder.Property(e => e.Vendor).HasMaxLength(200);
        builder.HasIndex(e => e.Date);
    }
}

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Number).HasMaxLength(50).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.Notes).HasMaxLength(2000);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.ClientId);

        builder.HasMany(i => i.LineItems)
            .WithOne()
            .HasForeignKey(li => li.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(i => i.LineItems).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Ignore(i => i.Total);
    }
}

internal sealed class InvoiceLineItemConfiguration : IEntityTypeConfiguration<InvoiceLineItem>
{
    public void Configure(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("invoice_line_items");
        builder.HasKey(li => li.Id);
        builder.Property(li => li.Description).HasMaxLength(500).IsRequired();
        builder.Property(li => li.UnitPrice).HasColumnType("numeric(18,2)");
        builder.Ignore(li => li.LineTotal);
    }
}
```

- [ ] **Step 3: Create the DbContext**

Create `api/SeeSharp.Infrastructure/Persistence/AppDbContext.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using SeeSharp.Application.Abstractions;
using SeeSharp.Domain.Entities;

namespace SeeSharp.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

- [ ] **Step 4: Create the DI extension**

Create `api/SeeSharp.Infrastructure/DependencyInjection.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeeSharp.Application.Abstractions;
using SeeSharp.Infrastructure.Persistence;

namespace SeeSharp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        return services;
    }
}
```

- [ ] **Step 5: Build**

Run: `dotnet build api/SeeSharp.Infrastructure`
Expected: succeeds.

- [ ] **Step 6: Commit**

```bash
git add api/SeeSharp.Infrastructure
git commit -m "feat(infrastructure): add AppDbContext, configurations, and DI"
```

### Task 3.2: Start Postgres and create the initial migration

**Files:**
- Create: `deploy/docker-compose.yml` (Postgres service only for now)
- Create: `api/SeeSharp.Infrastructure/Migrations/*` (generated)
- Create: `api/SeeSharp.Api/appsettings.Development.json` (connection string)

**Interfaces:**
- Produces: an initial EF migration and a running Postgres the API can connect to.

- [ ] **Step 1: Create the Postgres compose file**

Create `deploy/docker-compose.yml`:
```yaml
services:
  postgres:
    image: postgres:17
    container_name: seesharp-postgres
    environment:
      POSTGRES_USER: seesharp
      POSTGRES_PASSWORD: seesharp
      POSTGRES_DB: seesharp
    ports:
      - "5432:5432"
    volumes:
      - seesharp-pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U seesharp"]
      interval: 5s
      timeout: 5s
      retries: 5

volumes:
  seesharp-pgdata:
```

- [ ] **Step 2: Start Postgres**

Run from `deploy/`:
```bash
docker compose up -d postgres
```
Expected: container `seesharp-postgres` is healthy after a few seconds (`docker compose ps`).

- [ ] **Step 3: Set the development connection string**

Create `api/SeeSharp.Api/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "AppDb": "Host=localhost;Port=5432;Database=seesharp;Username=seesharp;Password=seesharp"
  },
  "Auth": {
    "Token": "dev-secret-token"
  },
  "Otel": {
    "Endpoint": "http://localhost:4317"
  }
}
```

- [ ] **Step 4: Install the EF CLI tool**

Run:
```bash
dotnet tool install --global dotnet-ef
```
Expected: installs `dotnet-ef`. If already installed, use `dotnet tool update --global dotnet-ef`. Ensure `~/.dotnet/tools` is on `PATH`.

- [ ] **Step 5: Reference Infrastructure from Api so the migration has a startup project**

The Api already references Infrastructure (Task 0.2). Confirm `dotnet build api` succeeds so EF can discover `AppDbContext`.

- [ ] **Step 6: Create the initial migration**

Run from `api/`:
```bash
dotnet ef migrations add InitialCreate \
  --project SeeSharp.Infrastructure \
  --startup-project SeeSharp.Api \
  --output-dir Migrations
```
Expected: a `Migrations/` folder appears under `SeeSharp.Infrastructure` with `InitialCreate` files.

Note: this requires the Api `Program.cs` to build a host EF can inspect. If the default template `Program.cs` has no `AddInfrastructure` call yet, the migration can still be created because EF uses the design-time `AppDbContext`. If EF cannot find the context, add a temporary `IDesignTimeDbContextFactory<AppDbContext>` in Infrastructure that reads the same connection string; keep it if useful.

- [ ] **Step 7: Commit**

```bash
git add deploy/docker-compose.yml api/SeeSharp.Infrastructure/Migrations api/SeeSharp.Api/appsettings.Development.json
git commit -m "feat(infrastructure): add postgres compose and initial migration"
```

### Task 3.3: Migration applier and seed data

**Files:**
- Create: `api/SeeSharp.Infrastructure/Persistence/DbInitializer.cs`

**Interfaces:**
- Produces: `Task DbInitializer.InitializeAsync(AppDbContext db, CancellationToken ct)` that applies pending migrations and seeds a few clients, categories, invoices, and expenses if the database is empty.

- [ ] **Step 1: Create the initializer**

Create `api/SeeSharp.Infrastructure/Persistence/DbInitializer.cs`:
```csharp
using Microsoft.EntityFrameworkCore;
using SeeSharp.Domain.Entities;

namespace SeeSharp.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (await db.Clients.AnyAsync(ct)) return;

        var acme = Client.Create("Acme Co", "billing@acme.test", "1 Acme Way");
        var globex = Client.Create("Globex", "ap@globex.test", null);
        db.Clients.AddRange(acme, globex);

        var software = Category.Create("Software");
        var travel = Category.Create("Travel");
        db.Categories.AddRange(software, travel);

        var invoice = Invoice.Create(acme.Id, "INV-1001",
            new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30), "Thanks for your business.");
        invoice.AddLineItem("Consulting", 10, 150m);
        invoice.AddLineItem("Setup fee", 1, 500m);
        invoice.MarkAsSent();
        invoice.MarkAsPaid();
        db.Invoices.Add(invoice);

        db.Expenses.Add(Expense.Create("JetBrains license", 199m, new DateOnly(2026, 6, 5), "JetBrains", software.Id));
        db.Expenses.Add(Expense.Create("Client visit", 85.50m, new DateOnly(2026, 6, 12), "Uber", travel.Id));

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 2: Build**

Run: `dotnet build api/SeeSharp.Infrastructure`
Expected: succeeds.

- [ ] **Step 3: Commit**

```bash
git add api/SeeSharp.Infrastructure
git commit -m "feat(infrastructure): add migration applier and seed data"
```

---

## Phase 4: API (endpoints, auth, errors, Swagger)

### Task 4.1: Program.cs wiring, config, ProblemDetails, and auth middleware

**Files:**
- Modify: `api/SeeSharp.Api/Program.cs`
- Create: `api/SeeSharp.Api/Auth/TokenAuthMiddleware.cs`
- Create: `api/SeeSharp.Api/appsettings.json`
- Modify: `api/SeeSharp.Api/SeeSharp.Api.csproj`

**Interfaces:**
- Consumes: `AddInfrastructure`, `DbInitializer`, all validators (via FluentValidation assembly scan).
- Produces: a running API with DI wired, a global exception handler returning `ProblemDetails`, token auth on all `/` API routes except Swagger and health, and Swagger UI. Exposes a `WebApplication` that later endpoint tasks call `Map*` on.

- [ ] **Step 1: Add API packages**

Run from `api/`:
```bash
dotnet add SeeSharp.Api package FluentValidation.DependencyInjectionExtensions
dotnet add SeeSharp.Api package Swashbuckle.AspNetCore
dotnet add SeeSharp.Api package Microsoft.AspNetCore.OpenApi
```

- [ ] **Step 2: Create the auth middleware**

Create `api/SeeSharp.Api/Auth/TokenAuthMiddleware.cs`:
```csharp
namespace SeeSharp.Api.Auth;

public sealed class TokenAuthMiddleware(RequestDelegate next, IConfiguration config)
{
    private readonly string _token = config["Auth:Token"]
        ?? throw new InvalidOperationException("Auth:Token is not configured.");

    private static readonly string[] OpenPrefixes = ["/swagger", "/health", "/openapi"];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        if (OpenPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var header = context.Request.Headers.Authorization.ToString();
        var provided = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? header["Bearer ".Length..].Trim()
            : null;

        if (provided != _token)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }

        await next(context);
    }
}
```

- [ ] **Step 3: Create base appsettings**

Create `api/SeeSharp.Api/appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "AppDb": ""
  },
  "Auth": {
    "Token": ""
  },
  "Otel": {
    "Endpoint": "http://localhost:4317"
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  }
}
```

- [ ] **Step 4: Rewrite Program.cs**

Replace `api/SeeSharp.Api/Program.cs` with:
```csharp
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SeeSharp.Api.Auth;
using SeeSharp.Application.Clients;
using SeeSharp.Infrastructure;
using SeeSharp.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AppDb")
    ?? throw new InvalidOperationException("ConnectionStrings:AppDb is not configured.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddValidatorsFromAssemblyContaining<CreateClientRequestValidator>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p => p.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseMiddleware<TokenAuthMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Endpoint groups are mapped here by later tasks:
// app.MapClients();
// app.MapCategories();
// app.MapExpenses();
// app.MapInvoices();
// app.MapReports();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(db);
}

app.Run();

public partial class Program { }
```

Note: `public partial class Program { }` at the end makes the entry point visible to the integration test project.

- [ ] **Step 5: Set the API URL**

Edit `api/SeeSharp.Api/Properties/launchSettings.json` so the `applicationUrl` for the `http` profile is `http://localhost:5080`. If the file or profile is missing, create the profile with that URL.

- [ ] **Step 6: Run the API**

Ensure Postgres is running (`docker compose ps` in `deploy/`), then run from `api/`:
```bash
dotnet run --project SeeSharp.Api
```
Expected: the app starts, applies migrations, seeds data, and listens on `http://localhost:5080`. Visit `http://localhost:5080/health` and confirm `{"status":"ok"}`. Visit `http://localhost:5080/swagger` and confirm the UI loads. Stop with Ctrl+C.

- [ ] **Step 7: Commit**

```bash
git add api/SeeSharp.Api
git commit -m "feat(api): wire DI, problemdetails, token auth, swagger, and db init"
```

### Task 4.2: Global exception to ProblemDetails mapping

**Files:**
- Create: `api/SeeSharp.Api/ExceptionHandling/DomainExceptionHandler.cs`
- Modify: `api/SeeSharp.Api/Program.cs`

**Interfaces:**
- Consumes: `InvalidInvoiceTransitionException`, `ValidationException` (FluentValidation), `ArgumentException`.
- Produces: an `IExceptionHandler` that maps domain and validation exceptions to `ProblemDetails` with correct status codes (409 for invalid transitions, 400 for validation and argument errors, 500 otherwise).

- [ ] **Step 1: Create the handler**

Create `api/SeeSharp.Api/ExceptionHandling/DomainExceptionHandler.cs`:
```csharp
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SeeSharp.Domain.Exceptions;

namespace SeeSharp.Api.ExceptionHandling;

public sealed class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ValidationException ve => (StatusCodes.Status400BadRequest, "Validation failed",
                string.Join("; ", ve.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"))),
            InvalidInvoiceTransitionException ite => (StatusCodes.Status409Conflict, "Invalid invoice transition", ite.Message),
            ArgumentException ae => (StatusCodes.Status400BadRequest, "Invalid request", ae.Message),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", "An unexpected error occurred.")
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://httpstatuses.io/{status}"
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
```

- [ ] **Step 2: Register the handler**

In `api/SeeSharp.Api/Program.cs`, replace `builder.Services.AddProblemDetails();` with:
```csharp
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<SeeSharp.Api.ExceptionHandling.DomainExceptionHandler>();
```

- [ ] **Step 3: Build**

Run: `dotnet build api/SeeSharp.Api`
Expected: succeeds.

- [ ] **Step 4: Commit**

```bash
git add api/SeeSharp.Api
git commit -m "feat(api): map domain and validation errors to problemdetails"
```

### Task 4.3: Validation helper and Clients endpoints

**Files:**
- Create: `api/SeeSharp.Api/Endpoints/ValidationExtensions.cs`
- Create: `api/SeeSharp.Api/Endpoints/ClientsEndpoints.cs`
- Modify: `api/SeeSharp.Api/Program.cs` (uncomment `app.MapClients();`)

**Interfaces:**
- Consumes: `IValidator<T>`, `ClientHandlers`, `IAppDbContext`.
- Produces:
  - `ValidationExtensions.ValidateAndThrowAsync<T>(this IValidator<T> validator, T instance, CancellationToken ct)` that throws `ValidationException` on failure.
  - `WebApplication MapClients(this WebApplication app)` registering the client routes under `/clients`.

- [ ] **Step 1: Create the validation helper**

Create `api/SeeSharp.Api/Endpoints/ValidationExtensions.cs`:
```csharp
using FluentValidation;

namespace SeeSharp.Api.Endpoints;

public static class ValidationExtensions
{
    public static async Task ValidateAndThrowAsync<T>(
        this IValidator<T> validator, T instance, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(instance, ct);
        if (!result.IsValid) throw new ValidationException(result.Errors);
    }
}
```

- [ ] **Step 2: Create the Clients endpoints**

Create `api/SeeSharp.Api/Endpoints/ClientsEndpoints.cs`:
```csharp
using FluentValidation;
using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Clients;

namespace SeeSharp.Api.Endpoints;

public static class ClientsEndpoints
{
    public static WebApplication MapClients(this WebApplication app)
    {
        var group = app.MapGroup("/clients").WithTags("Clients");

        group.MapGet("/", async (IAppDbContext db, int page, int pageSize, CancellationToken ct)
            => Results.Ok(await ClientHandlers.ListAsync(db, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize, ct)));

        group.MapGet("/{id:guid}", async (IAppDbContext db, Guid id, CancellationToken ct) =>
        {
            var client = await ClientHandlers.GetAsync(db, id, ct);
            return client is null ? Results.NotFound() : Results.Ok(client);
        });

        group.MapPost("/", async (
            IAppDbContext db, IValidator<CreateClientRequest> validator, CreateClientRequest req, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            var created = await ClientHandlers.CreateAsync(db, req, ct);
            return Results.Created($"/clients/{created.Id}", created);
        });

        group.MapPut("/{id:guid}", async (
            IAppDbContext db, IValidator<UpdateClientRequest> validator, Guid id, UpdateClientRequest req, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            var updated = await ClientHandlers.UpdateAsync(db, id, req, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        group.MapDelete("/{id:guid}", async (IAppDbContext db, Guid id, CancellationToken ct) =>
            await ClientHandlers.DeleteAsync(db, id, ct) ? Results.NoContent() : Results.NotFound());

        return app;
    }
}
```

- [ ] **Step 3: Map the group in Program.cs**

In `api/SeeSharp.Api/Program.cs`, uncomment/insert `app.MapClients();` where the endpoint groups are mapped, and add `using SeeSharp.Api.Endpoints;` at the top.

- [ ] **Step 4: Run and smoke test**

Run the API (`dotnet run --project SeeSharp.Api` from `api/`, Postgres up). In another terminal:
```bash
curl -s -H "Authorization: Bearer dev-secret-token" http://localhost:5080/clients | head
curl -s -X POST -H "Authorization: Bearer dev-secret-token" -H "Content-Type: application/json" \
  -d '{"name":"Test Client","email":"t@test.dev","address":null}' http://localhost:5080/clients
```
Expected: the GET returns a paged list including seeded clients; the POST returns 201 with the created client. A request without the header returns 401.

- [ ] **Step 5: Commit**

```bash
git add api/SeeSharp.Api
git commit -m "feat(api): add clients endpoints and validation helper"
```

### Task 4.4: Categories, Expenses, Invoices, and Reports endpoints

**Files:**
- Create: `api/SeeSharp.Api/Endpoints/CategoriesEndpoints.cs`
- Create: `api/SeeSharp.Api/Endpoints/ExpensesEndpoints.cs`
- Create: `api/SeeSharp.Api/Endpoints/InvoicesEndpoints.cs`
- Create: `api/SeeSharp.Api/Endpoints/ReportsEndpoints.cs`
- Modify: `api/SeeSharp.Api/Program.cs`

**Interfaces:**
- Consumes: the corresponding handlers and validators.
- Produces: `MapCategories`, `MapExpenses`, `MapInvoices`, `MapReports` extension methods, each returning `WebApplication`.

- [ ] **Step 1: Categories endpoints**

Create `api/SeeSharp.Api/Endpoints/CategoriesEndpoints.cs`:
```csharp
using FluentValidation;
using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Categories;

namespace SeeSharp.Api.Endpoints;

public static class CategoriesEndpoints
{
    public static WebApplication MapCategories(this WebApplication app)
    {
        var group = app.MapGroup("/categories").WithTags("Categories");

        group.MapGet("/", async (IAppDbContext db, CancellationToken ct)
            => Results.Ok(await CategoryHandlers.ListAsync(db, ct)));

        group.MapPost("/", async (IAppDbContext db, CreateCategoryRequest req, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return Results.BadRequest(new { error = "Name is required." });
            var created = await CategoryHandlers.CreateAsync(db, req, ct);
            return Results.Created($"/categories/{created.Id}", created);
        });

        group.MapDelete("/{id:guid}", async (IAppDbContext db, Guid id, CancellationToken ct) =>
            await CategoryHandlers.DeleteAsync(db, id, ct) ? Results.NoContent() : Results.NotFound());

        return app;
    }
}
```

- [ ] **Step 2: Expenses endpoints**

Create `api/SeeSharp.Api/Endpoints/ExpensesEndpoints.cs`:
```csharp
using FluentValidation;
using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Expenses;

namespace SeeSharp.Api.Endpoints;

public static class ExpensesEndpoints
{
    public static WebApplication MapExpenses(this WebApplication app)
    {
        var group = app.MapGroup("/expenses").WithTags("Expenses");

        group.MapGet("/", async (
            IAppDbContext db, Guid? categoryId, DateOnly? from, DateOnly? to,
            int page, int pageSize, CancellationToken ct)
            => Results.Ok(await ExpenseHandlers.ListAsync(
                db, categoryId, from, to, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize, ct)));

        group.MapGet("/{id:guid}", async (IAppDbContext db, Guid id, CancellationToken ct) =>
        {
            var expense = await ExpenseHandlers.GetAsync(db, id, ct);
            return expense is null ? Results.NotFound() : Results.Ok(expense);
        });

        group.MapPost("/", async (
            IAppDbContext db, IValidator<CreateExpenseRequest> validator, CreateExpenseRequest req, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            var created = await ExpenseHandlers.CreateAsync(db, req, ct);
            return Results.Created($"/expenses/{created.Id}", created);
        });

        group.MapPut("/{id:guid}", async (
            IAppDbContext db, IValidator<UpdateExpenseRequest> validator, Guid id, UpdateExpenseRequest req, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            var updated = await ExpenseHandlers.UpdateAsync(db, id, req, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        group.MapDelete("/{id:guid}", async (IAppDbContext db, Guid id, CancellationToken ct) =>
            await ExpenseHandlers.DeleteAsync(db, id, ct) ? Results.NoContent() : Results.NotFound());

        return app;
    }
}
```

- [ ] **Step 3: Invoices endpoints**

Create `api/SeeSharp.Api/Endpoints/InvoicesEndpoints.cs`:
```csharp
using FluentValidation;
using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Invoices;
using SeeSharp.Domain.Enums;

namespace SeeSharp.Api.Endpoints;

public static class InvoicesEndpoints
{
    public static WebApplication MapInvoices(this WebApplication app)
    {
        var group = app.MapGroup("/invoices").WithTags("Invoices");

        group.MapGet("/", async (
            IAppDbContext db, string? status, Guid? clientId, int page, int pageSize, CancellationToken ct) =>
        {
            InvoiceStatus? parsed = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<InvoiceStatus>(status, ignoreCase: true, out var s))
                    return Results.BadRequest(new { error = $"Unknown status '{status}'." });
                parsed = s;
            }
            return Results.Ok(await InvoiceHandlers.ListAsync(
                db, parsed, clientId, page == 0 ? 1 : page, pageSize == 0 ? 20 : pageSize, ct));
        });

        group.MapGet("/{id:guid}", async (IAppDbContext db, Guid id, CancellationToken ct) =>
        {
            var invoice = await InvoiceHandlers.GetAsync(db, id, ct);
            return invoice is null ? Results.NotFound() : Results.Ok(invoice);
        });

        group.MapPost("/", async (
            IAppDbContext db, IValidator<CreateInvoiceRequest> validator, CreateInvoiceRequest req, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            var created = await InvoiceHandlers.CreateAsync(db, req, ct);
            return Results.Created($"/invoices/{created.Id}", created);
        });

        group.MapPut("/{id:guid}", async (
            IAppDbContext db, IValidator<UpdateInvoiceRequest> validator, Guid id, UpdateInvoiceRequest req, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            var updated = await InvoiceHandlers.UpdateAsync(db, id, req, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        group.MapPost("/{id:guid}/status", async (
            IAppDbContext db, Guid id, ChangeStatusRequest req, CancellationToken ct) =>
        {
            var updated = await InvoiceHandlers.ChangeStatusAsync(db, id, req.Status, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        group.MapDelete("/{id:guid}", async (IAppDbContext db, Guid id, CancellationToken ct) =>
            await InvoiceHandlers.DeleteAsync(db, id, ct) ? Results.NoContent() : Results.NotFound());

        return app;
    }
}
```

- [ ] **Step 4: Reports endpoints**

Create `api/SeeSharp.Api/Endpoints/ReportsEndpoints.cs`:
```csharp
using SeeSharp.Application.Abstractions;
using SeeSharp.Application.Reports;

namespace SeeSharp.Api.Endpoints;

public static class ReportsEndpoints
{
    public static WebApplication MapReports(this WebApplication app)
    {
        var group = app.MapGroup("/reports").WithTags("Reports");

        group.MapGet("/summary", async (IAppDbContext db, DateOnly? from, DateOnly? to, CancellationToken ct) =>
        {
            var fromDate = from ?? new DateOnly(DateTime.UtcNow.Year, 1, 1);
            var toDate = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
            if (toDate < fromDate)
                return Results.BadRequest(new { error = "'to' cannot be before 'from'." });
            return Results.Ok(await ReportHandlers.GetSummaryAsync(db, fromDate, toDate, ct));
        });

        return app;
    }
}
```

- [ ] **Step 5: Map all groups in Program.cs**

In `api/SeeSharp.Api/Program.cs`, ensure these are present where groups are mapped:
```csharp
app.MapClients();
app.MapCategories();
app.MapExpenses();
app.MapInvoices();
app.MapReports();
```

- [ ] **Step 6: Run and smoke test the full surface**

Run the API. With the bearer header, GET `/invoices`, `/expenses`, `/categories`, and `/reports/summary?from=2026-01-01&to=2026-12-31`. Confirm seeded data appears and the report shows income and expenses. Try `POST /invoices/{id}/status` with `{"status":"paid"}` on the seeded (already paid) invoice and confirm a 409 ProblemDetails.

- [ ] **Step 7: Commit**

```bash
git add api/SeeSharp.Api
git commit -m "feat(api): add categories, expenses, invoices, and reports endpoints"
```

### Task 4.5: API integration tests with Testcontainers

**Files:**
- Modify: `api/SeeSharp.Api.Tests/SeeSharp.Api.Tests.csproj`
- Create: `api/SeeSharp.Api.Tests/ApiFactory.cs`
- Create: `api/SeeSharp.Api.Tests/ClientsApiTests.cs`
- Create: `api/SeeSharp.Api.Tests/InvoicesApiTests.cs`

**Interfaces:**
- Consumes: `Program` (via `WebApplicationFactory<Program>`), the running API, a throwaway Postgres container.
- Produces: `ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime` that starts a Postgres container and overrides the connection string and auth token; integration tests for clients CRUD and an invoice illegal-transition 409.

- [ ] **Step 1: Add test packages**

Run from `api/`:
```bash
dotnet add SeeSharp.Api.Tests package Microsoft.AspNetCore.Mvc.Testing
dotnet add SeeSharp.Api.Tests package Testcontainers.PostgreSql
dotnet add SeeSharp.Api.Tests package FluentAssertions
```

- [ ] **Step 2: Create the API factory**

Create `api/SeeSharp.Api.Tests/ApiFactory.cs`:
```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace SeeSharp.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .Build();

    public const string Token = "test-token";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:AppDb", _db.GetConnectionString());
        builder.UseSetting("Auth:Token", Token);
        builder.UseEnvironment("Development");
    }

    public async Task InitializeAsync() => await _db.StartAsync();

    public new async Task DisposeAsync() => await _db.DisposeAsync();
}
```

- [ ] **Step 3: Create the clients integration tests**

Create `api/SeeSharp.Api.Tests/ClientsApiTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace SeeSharp.Api.Tests;

public class ClientsApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private HttpClient Client()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiFactory.Token);
        return client;
    }

    [Fact]
    public async Task Post_then_Get_returns_created_client()
    {
        var client = Client();
        var create = await client.PostAsJsonAsync("/clients",
            new { name = "Integration Client", email = "i@test.dev", address = (string?)null });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await create.Content.ReadFromJsonAsync<ClientDto>();
        created!.Name.Should().Be("Integration Client");

        var get = await client.GetAsync($"/clients/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_without_token_is_unauthorized()
    {
        var res = await factory.CreateClient().GetAsync("/clients");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record ClientDto(Guid Id, string Name, string? Email, string? Address, DateTimeOffset CreatedAt);
}
```

- [ ] **Step 4: Create the invoice transition test**

Create `api/SeeSharp.Api.Tests/InvoicesApiTests.cs`:
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;

namespace SeeSharp.Api.Tests;

public class InvoicesApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private HttpClient Client()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiFactory.Token);
        return client;
    }

    [Fact]
    public async Task Marking_a_draft_invoice_paid_returns_409()
    {
        var client = Client();

        var clientRes = await client.PostAsJsonAsync("/clients",
            new { name = "Inv Client", email = (string?)null, address = (string?)null });
        var created = await clientRes.Content.ReadFromJsonAsync<IdOnly>();

        var invoiceRes = await client.PostAsJsonAsync("/invoices", new
        {
            clientId = created!.Id,
            number = "INV-T1",
            issueDate = "2026-07-01",
            dueDate = "2026-07-31",
            notes = (string?)null,
            lineItems = new[] { new { description = "Work", quantity = 1, unitPrice = 100.0 } }
        });
        invoiceRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var invoice = await invoiceRes.Content.ReadFromJsonAsync<IdOnly>();

        var statusRes = await client.PostAsJsonAsync($"/invoices/{invoice!.Id}/status", new { status = "paid" });
        statusRes.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private sealed record IdOnly(Guid Id);
}
```

- [ ] **Step 5: Run the integration tests**

Run from `api/`:
```bash
dotnet test SeeSharp.Api.Tests
```
Expected: all pass. Testcontainers pulls `postgres:17` and starts a throwaway container (Docker must be running).

- [ ] **Step 6: Run the whole suite**

Run from `api/`:
```bash
dotnet test
```
Expected: domain and API tests all pass.

- [ ] **Step 7: Commit**

```bash
git add api/SeeSharp.Api.Tests
git commit -m "test(api): add integration tests with testcontainers"
```

---

## Phase 5: Telemetry (OpenTelemetry to SigNoz)

### Task 5.1: Wire OpenTelemetry traces, metrics, and logs

**Files:**
- Modify: `api/SeeSharp.Infrastructure/SeeSharp.Infrastructure.csproj`
- Create: `api/SeeSharp.Infrastructure/Telemetry/TelemetryExtensions.cs`
- Create: `api/SeeSharp.Infrastructure/Telemetry/AppMetrics.cs`
- Modify: `api/SeeSharp.Api/Program.cs`

**Interfaces:**
- Produces:
  - `IServiceCollection AddSeeSharpTelemetry(this IServiceCollection services, IConfiguration config, string serviceName)` configuring OTel traces (ASP.NET Core + EF Core + HttpClient + the app's own `ActivitySource`), metrics (ASP.NET Core + runtime + a custom meter), and logs, all exporting OTLP to `Otel:Endpoint`.
  - `AppMetrics` with a `Meter` named `SeeSharp.Api` and a counter `invoices_created`, registered in DI as a singleton, incremented in the invoice create endpoint.
  - `AppTelemetry.ActivitySource`, a static `ActivitySource` named `SeeSharp.Api`, used to create a manual span named `invoice.status_change`. This is the hand-rolled span the spec calls for, and it teaches the manual tracing path alongside the auto-instrumentation.

- [ ] **Step 1: Add OTel packages**

Run from `api/`:
```bash
dotnet add SeeSharp.Infrastructure package OpenTelemetry.Extensions.Hosting
dotnet add SeeSharp.Infrastructure package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add SeeSharp.Infrastructure package OpenTelemetry.Instrumentation.AspNetCore
dotnet add SeeSharp.Infrastructure package OpenTelemetry.Instrumentation.EntityFrameworkCore
dotnet add SeeSharp.Infrastructure package OpenTelemetry.Instrumentation.Http
dotnet add SeeSharp.Infrastructure package OpenTelemetry.Instrumentation.Runtime
```

- [ ] **Step 2: Create the custom metrics holder**

Create `api/SeeSharp.Infrastructure/Telemetry/AppMetrics.cs`:
```csharp
using System.Diagnostics.Metrics;

namespace SeeSharp.Infrastructure.Telemetry;

public sealed class AppMetrics
{
    public const string MeterName = "SeeSharp.Api";
    private readonly Counter<long> _invoicesCreated;

    public AppMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);
        _invoicesCreated = meter.CreateCounter<long>("invoices_created", description: "Number of invoices created.");
    }

    public void InvoiceCreated() => _invoicesCreated.Add(1);
}
```

Also create `api/SeeSharp.Infrastructure/Telemetry/AppTelemetry.cs` to hold the shared `ActivitySource` used for manual spans:
```csharp
using System.Diagnostics;

namespace SeeSharp.Infrastructure.Telemetry;

public static class AppTelemetry
{
    public const string ActivitySourceName = "SeeSharp.Api";
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
```

- [ ] **Step 3: Create the telemetry extension**

Create `api/SeeSharp.Infrastructure/Telemetry/TelemetryExtensions.cs`:
```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SeeSharp.Infrastructure.Telemetry;

public static class TelemetryExtensions
{
    public static IServiceCollection AddSeeSharpTelemetry(
        this IServiceCollection services, IConfiguration config, string serviceName)
    {
        var endpoint = new Uri(config["Otel:Endpoint"] ?? "http://localhost:4317");

        services.AddSingleton<AppMetrics>();

        var resource = ResourceBuilder.CreateDefault()
            .AddService(serviceName: serviceName, serviceVersion: "1.0.0")
            .AddAttributes(new KeyValuePair<string, object>[]
            {
                new("deployment.environment", config["ASPNETCORE_ENVIRONMENT"] ?? "Development")
            });

        services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(serviceName, serviceVersion: "1.0.0"))
            .WithTracing(tracing => tracing
                .AddSource(AppTelemetry.ActivitySourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation(o => o.SetDbStatementForText = true)
                .AddOtlpExporter(o => o.Endpoint = endpoint))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(AppMetrics.MeterName)
                .AddOtlpExporter(o => o.Endpoint = endpoint));

        services.AddLogging(logging => logging.AddOpenTelemetry(o =>
        {
            o.SetResourceBuilder(resource);
            o.IncludeScopes = true;
            o.AddOtlpExporter(e => e.Endpoint = endpoint);
        }));

        return services;
    }
}
```

- [ ] **Step 4: Call it from Program.cs and increment the counter**

In `api/SeeSharp.Api/Program.cs`, after `AddInfrastructure(...)`, add:
```csharp
builder.Services.AddSeeSharpTelemetry(builder.Configuration, "see-sharp-api");
```
with `using SeeSharp.Infrastructure.Telemetry;` at the top.

Then update the invoice create endpoint in `api/SeeSharp.Api/Endpoints/InvoicesEndpoints.cs` so the POST handler takes `AppMetrics metrics` and calls `metrics.InvoiceCreated();` after a successful create:
```csharp
        group.MapPost("/", async (
            IAppDbContext db, IValidator<CreateInvoiceRequest> validator, AppMetrics metrics,
            CreateInvoiceRequest req, CancellationToken ct) =>
        {
            await validator.ValidateAndThrowAsync(req, ct);
            var created = await InvoiceHandlers.CreateAsync(db, req, ct);
            metrics.InvoiceCreated();
            return Results.Created($"/invoices/{created.Id}", created);
        });
```
Add `using SeeSharp.Infrastructure.Telemetry;` to that file.

- [ ] **Step 5: Add a manual span to the status-change endpoint**

The spec asks for one hand-rolled span so the manual tracing path is demonstrated, not just auto-instrumentation. Wrap the status change in an `invoice.status_change` span. In `api/SeeSharp.Api/Endpoints/InvoicesEndpoints.cs`, replace the status-change endpoint with:
```csharp
        group.MapPost("/{id:guid}/status", async (
            IAppDbContext db, Guid id, ChangeStatusRequest req, CancellationToken ct) =>
        {
            using var activity = AppTelemetry.ActivitySource.StartActivity("invoice.status_change");
            activity?.SetTag("invoice.id", id);
            activity?.SetTag("invoice.target_status", req.Status);

            var updated = await InvoiceHandlers.ChangeStatusAsync(db, id, req.Status, ct);
            activity?.SetTag("invoice.found", updated is not null);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });
```
The `using SeeSharp.Infrastructure.Telemetry;` added in Step 4 covers `AppTelemetry`. When an illegal transition throws, the exception propagates to the global handler (409) and the span records the error automatically because it is still open at throw time.

- [ ] **Step 6: Build and test**

Run from `api/`:
```bash
dotnet build && dotnet test
```
Expected: build succeeds; all tests still pass. Telemetry export failures (no collector) must not fail requests.

- [ ] **Step 7: Run and confirm the app still serves with no collector**

Run the API without SigNoz running. Hit `/health` and `/clients`. Confirm the app works and only logs exporter connection warnings, nothing fatal.

- [ ] **Step 8: Commit**

```bash
git add api/SeeSharp.Infrastructure api/SeeSharp.Api
git commit -m "feat(telemetry): add opentelemetry traces, metrics, and logs"
```

---

## Phase 6: Deploy (SigNoz, Dockerfiles, full compose)

### Task 6.1: Add the SigNoz stack

**Files:**
- Create: `deploy/signoz/README.md`
- Create: `deploy/signoz/docker-compose.yml` (vendored from the official SigNoz compose, pinned)

**Interfaces:**
- Produces: a self-hosted SigNoz stack the API can export OTLP to on `localhost:4317`.

- [ ] **Step 1: Vendor the official SigNoz compose**

Fetch the current official SigNoz docker-compose for self-hosting and save it to `deploy/signoz/docker-compose.yml`. Use the pinned compose from the SigNoz repository `signoz/deploy/docker` for the latest stable release. Confirm the OTLP collector publishes ports `4317` (gRPC) and `4318` (HTTP) and the UI publishes `3301`. If the upstream uses different published ports, record the actual ports in the README and update `Otel:Endpoint` accordingly.

- [ ] **Step 2: Write the SigNoz readme**

Create `deploy/signoz/README.md` explaining: what the stack is, how to start it (`docker compose -f deploy/signoz/docker-compose.yml up -d`), the UI URL, the OTLP endpoint the API uses, expected memory use, and how to stop and remove it. Plain human voice, no em dashes.

- [ ] **Step 3: Start SigNoz and confirm the UI**

Run:
```bash
docker compose -f deploy/signoz/docker-compose.yml up -d
```
Expected: containers come up. Open the SigNoz UI (default `http://localhost:3301`) and confirm it loads. This can take a minute on first run.

- [ ] **Step 4: Generate traffic and confirm telemetry lands**

With Postgres and SigNoz up, run the API, then hit several endpoints (list clients, create an invoice, get the report). In the SigNoz UI, confirm the `see-sharp-api` service appears with traces, that a trace shows child DB spans, and that logs and the `invoices_created` metric are visible. If nothing appears, verify `Otel:Endpoint` matches the collector's published port.

- [ ] **Step 5: Commit**

```bash
git add deploy/signoz
git commit -m "feat(deploy): add self-hosted signoz stack"
```

### Task 6.2: Dockerfiles and full app compose

**Files:**
- Create: `api/Dockerfile`
- Create: `api/.dockerignore`
- Create: `web/Dockerfile` (created here, used after Phase 7)
- Create: `web/.dockerignore`
- Modify: `deploy/docker-compose.yml` (add api and web services)

**Interfaces:**
- Produces: a containerized API and web, and a compose file that runs Postgres, the API, and the web together, with the API pointed at Postgres and the SigNoz collector by environment variables.

- [ ] **Step 1: Create the API Dockerfile**

Create `api/Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore SeeSharp.sln
RUN dotnet publish SeeSharp.Api/SeeSharp.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SeeSharp.Api.dll"]
```

- [ ] **Step 2: Create the API .dockerignore**

Create `api/.dockerignore`:
```
**/bin
**/obj
**/.vs
```

- [ ] **Step 3: Create the web Dockerfile**

Create `web/Dockerfile`:
```dockerfile
FROM node:22 AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

- [ ] **Step 4: Create the web .dockerignore and nginx config**

Create `web/.dockerignore`:
```
node_modules
dist
```
Create `web/nginx.conf`:
```
server {
    listen 80;
    location / {
        root /usr/share/nginx/html;
        try_files $uri $uri/ /index.html;
    }
}
```

- [ ] **Step 5: Extend the compose file**

Add to `deploy/docker-compose.yml` under `services:` (keep the existing `postgres` and `volumes`):
```yaml
  api:
    build:
      context: ../api
      dockerfile: Dockerfile
    container_name: seesharp-api
    depends_on:
      postgres:
        condition: service_healthy
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__AppDb: "Host=postgres;Port=5432;Database=seesharp;Username=seesharp;Password=seesharp"
      Auth__Token: "dev-secret-token"
      Otel__Endpoint: "http://host.docker.internal:4317"
      Cors__AllowedOrigins__0: "http://localhost:8081"
    ports:
      - "5080:8080"
    extra_hosts:
      - "host.docker.internal:host-gateway"

  web:
    build:
      context: ../web
      dockerfile: Dockerfile
    container_name: seesharp-web
    depends_on:
      - api
    ports:
      - "8081:80"
```

Note: `Otel__Endpoint` points at `host.docker.internal` because SigNoz runs from its own compose on the host. The API in Docker reaches the host collector that way.

- [ ] **Step 6: Build the API image**

Run from `deploy/`:
```bash
docker compose build api
```
Expected: the multi-stage build succeeds and produces the `seesharp-api` image. (The `web` build is exercised after Phase 7 once `web/` has a `package.json`.)

- [ ] **Step 7: Commit**

```bash
git add api/Dockerfile api/.dockerignore web/Dockerfile web/.dockerignore web/nginx.conf deploy/docker-compose.yml
git commit -m "feat(deploy): add dockerfiles and full app compose"
```

---

## Phase 7: React Frontend

### Task 7.1: Scaffold the Vite React TS app with routing and API client

**Files:**
- Create: `web/` Vite project (package.json, tsconfig, index.html, src/main.tsx, src/App.tsx)
- Create: `web/src/api/client.ts`
- Create: `web/src/api/types.ts`
- Create: `web/.env.example`
- Create: `web/vite.config.ts` (with dev proxy)

**Interfaces:**
- Produces: a running Vite dev app at `http://localhost:5173`, a typed `api` client that attaches the bearer token and parses `ProblemDetails`, and TypeScript types mirroring the API DTOs.

- [ ] **Step 1: Scaffold Vite**

Run from the repo root:
```bash
npm create vite@latest web -- --template react-ts
cd web
npm install
npm install react-router-dom
```

- [ ] **Step 2: Configure the dev proxy**

Replace `web/vite.config.ts`:
```ts
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: "http://localhost:5080",
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ""),
      },
    },
  },
});
```

- [ ] **Step 3: Create the API types**

Create `web/src/api/types.ts`:
```ts
export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface Client {
  id: string;
  name: string;
  email: string | null;
  address: string | null;
  createdAt: string;
}

export interface LineItem {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Invoice {
  id: string;
  clientId: string;
  number: string;
  status: string;
  issueDate: string;
  dueDate: string;
  notes: string | null;
  total: number;
  createdAt: string;
  lineItems: LineItem[];
}

export interface Expense {
  id: string;
  categoryId: string | null;
  description: string;
  amount: number;
  date: string;
  vendor: string | null;
  createdAt: string;
}

export interface Category {
  id: string;
  name: string;
}

export interface MonthlyRow {
  year: number;
  month: number;
  income: number;
  expenses: number;
  net: number;
}

export interface Summary {
  from: string;
  to: string;
  totalIncome: number;
  totalExpenses: number;
  net: number;
  months: MonthlyRow[];
}
```

- [ ] **Step 4: Create the API client**

Create `web/src/api/client.ts`:
```ts
const BASE = "/api";
const TOKEN = import.meta.env.VITE_API_TOKEN ?? "dev-secret-token";

export class ApiError extends Error {
  constructor(public status: number, message: string) {
    super(message);
  }
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${TOKEN}`,
      ...(options.headers ?? {}),
    },
  });

  if (!res.ok) {
    let detail = res.statusText;
    try {
      const body = await res.json();
      detail = body.detail ?? body.title ?? body.error ?? detail;
    } catch {
      // no body
    }
    throw new ApiError(res.status, detail);
  }

  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body: unknown) =>
    request<T>(path, { method: "POST", body: JSON.stringify(body) }),
  put: <T>(path: string, body: unknown) =>
    request<T>(path, { method: "PUT", body: JSON.stringify(body) }),
  del: (path: string) => request<void>(path, { method: "DELETE" }),
};
```

- [ ] **Step 5: Create the env example**

Create `web/.env.example`:
```
VITE_API_TOKEN=dev-secret-token
```

- [ ] **Step 6: Run the dev server**

Run from `web/`:
```bash
npm run dev
```
Expected: Vite serves at `http://localhost:5173`. The default page loads. Stop with Ctrl+C.

- [ ] **Step 7: Commit**

```bash
git add web
git commit -m "feat(web): scaffold vite react app with typed api client"
```

### Task 7.2: Router, layout, and the four pages

**Files:**
- Create: `web/src/main.tsx` (router setup, replacing default)
- Create: `web/src/App.tsx` (layout with nav)
- Create: `web/src/pages/Dashboard.tsx`
- Create: `web/src/pages/Clients.tsx`
- Create: `web/src/pages/Invoices.tsx`
- Create: `web/src/pages/Expenses.tsx`
- Create: `web/src/index.css` (minimal styling)

**Interfaces:**
- Consumes: `api`, the types, React Router.
- Produces: four working pages wired into routes with a shared nav layout. Each page fetches from the API and renders a table.

- [ ] **Step 1: Set up the router**

Replace `web/src/main.tsx`:
```tsx
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { createBrowserRouter, RouterProvider } from "react-router-dom";
import App from "./App";
import Dashboard from "./pages/Dashboard";
import Clients from "./pages/Clients";
import Invoices from "./pages/Invoices";
import Expenses from "./pages/Expenses";
import "./index.css";

const router = createBrowserRouter([
  {
    path: "/",
    element: <App />,
    children: [
      { index: true, element: <Dashboard /> },
      { path: "clients", element: <Clients /> },
      { path: "invoices", element: <Invoices /> },
      { path: "expenses", element: <Expenses /> },
    ],
  },
]);

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>
);
```

- [ ] **Step 2: Create the layout**

Replace `web/src/App.tsx`:
```tsx
import { NavLink, Outlet } from "react-router-dom";

export default function App() {
  return (
    <div className="layout">
      <header>
        <h1>See Sharp</h1>
        <nav>
          <NavLink to="/">Dashboard</NavLink>
          <NavLink to="/clients">Clients</NavLink>
          <NavLink to="/invoices">Invoices</NavLink>
          <NavLink to="/expenses">Expenses</NavLink>
        </nav>
      </header>
      <main>
        <Outlet />
      </main>
    </div>
  );
}
```

- [ ] **Step 3: Minimal styling**

Replace `web/src/index.css`:
```css
:root { font-family: system-ui, sans-serif; color: #1a1a1a; }
body { margin: 0; background: #f7f7f8; }
.layout { max-width: 960px; margin: 0 auto; padding: 1rem; }
header { display: flex; align-items: baseline; gap: 2rem; border-bottom: 1px solid #ddd; padding-bottom: 1rem; }
nav a { margin-right: 1rem; text-decoration: none; color: #444; }
nav a.active { font-weight: 600; color: #000; }
table { width: 100%; border-collapse: collapse; margin-top: 1rem; }
th, td { text-align: left; padding: 0.5rem; border-bottom: 1px solid #eee; }
.error { color: #b00020; }
```

- [ ] **Step 4: Dashboard page**

Create `web/src/pages/Dashboard.tsx`:
```tsx
import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { Summary } from "../api/types";

export default function Dashboard() {
  const [summary, setSummary] = useState<Summary | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const year = new Date().getFullYear();
    api
      .get<Summary>(`/reports/summary?from=${year}-01-01&to=${year}-12-31`)
      .then(setSummary)
      .catch((e) => setError(e.message));
  }, []);

  if (error) return <p className="error">{error}</p>;
  if (!summary) return <p>Loading...</p>;

  return (
    <section>
      <h2>This year</h2>
      <p>
        Income: {summary.totalIncome.toFixed(2)} | Expenses:{" "}
        {summary.totalExpenses.toFixed(2)} | Net: {summary.net.toFixed(2)}
      </p>
      <table>
        <thead>
          <tr><th>Month</th><th>Income</th><th>Expenses</th><th>Net</th></tr>
        </thead>
        <tbody>
          {summary.months.map((m) => (
            <tr key={`${m.year}-${m.month}`}>
              <td>{m.year}-{String(m.month).padStart(2, "0")}</td>
              <td>{m.income.toFixed(2)}</td>
              <td>{m.expenses.toFixed(2)}</td>
              <td>{m.net.toFixed(2)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
```

- [ ] **Step 5: Clients page**

Create `web/src/pages/Clients.tsx`:
```tsx
import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { Client, Paged } from "../api/types";

export default function Clients() {
  const [clients, setClients] = useState<Client[]>([]);
  const [name, setName] = useState("");
  const [error, setError] = useState<string | null>(null);

  const load = () =>
    api.get<Paged<Client>>("/clients").then((p) => setClients(p.items)).catch((e) => setError(e.message));

  useEffect(() => { load(); }, []);

  const add = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      await api.post("/clients", { name, email: null, address: null });
      setName("");
      await load();
    } catch (err) {
      setError((err as Error).message);
    }
  };

  return (
    <section>
      <h2>Clients</h2>
      <form onSubmit={add}>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Client name" />
        <button type="submit">Add</button>
      </form>
      {error && <p className="error">{error}</p>}
      <table>
        <thead><tr><th>Name</th><th>Email</th></tr></thead>
        <tbody>
          {clients.map((c) => (
            <tr key={c.id}><td>{c.name}</td><td>{c.email ?? ""}</td></tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
```

- [ ] **Step 6: Invoices and Expenses pages**

Create `web/src/pages/Invoices.tsx`:
```tsx
import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { Invoice, Paged } from "../api/types";

export default function Invoices() {
  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get<Paged<Invoice>>("/invoices").then((p) => setInvoices(p.items)).catch((e) => setError(e.message));
  }, []);

  if (error) return <p className="error">{error}</p>;

  return (
    <section>
      <h2>Invoices</h2>
      <table>
        <thead><tr><th>Number</th><th>Status</th><th>Total</th><th>Due</th></tr></thead>
        <tbody>
          {invoices.map((i) => (
            <tr key={i.id}>
              <td>{i.number}</td><td>{i.status}</td>
              <td>{i.total.toFixed(2)}</td><td>{i.dueDate}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
```

Create `web/src/pages/Expenses.tsx`:
```tsx
import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { Expense, Paged } from "../api/types";

export default function Expenses() {
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get<Paged<Expense>>("/expenses").then((p) => setExpenses(p.items)).catch((e) => setError(e.message));
  }, []);

  if (error) return <p className="error">{error}</p>;

  return (
    <section>
      <h2>Expenses</h2>
      <table>
        <thead><tr><th>Date</th><th>Description</th><th>Vendor</th><th>Amount</th></tr></thead>
        <tbody>
          {expenses.map((e) => (
            <tr key={e.id}>
              <td>{e.date}</td><td>{e.description}</td>
              <td>{e.vendor ?? ""}</td><td>{e.amount.toFixed(2)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </section>
  );
}
```

- [ ] **Step 7: Verify end to end**

Ensure Postgres is up and the API is running (`dotnet run --project SeeSharp.Api`). Run `npm run dev` in `web/`. Open `http://localhost:5173` and confirm the Dashboard shows the seeded summary, Clients lists Acme and Globex and can add a client, Invoices shows INV-1001 as Paid, and Expenses lists the two seeded expenses. Confirm `npm run build` succeeds with no type errors.

- [ ] **Step 8: Commit**

```bash
git add web
git commit -m "feat(web): add router, layout, and the four pages"
```

---

## Phase 8: Documentation

### Task 8.1: Write ARCHITECTURE.md

**Files:**
- Create: `docs/ARCHITECTURE.md`

**Interfaces:**
- Produces: the guided-tour project map described in the spec.

- [ ] **Step 1: Write the guided tour**

Create `docs/ARCHITECTURE.md` covering, in a plain human voice with no em dashes:
  - A short intro to what the project is and the three parts plus SigNoz.
  - A top-level folder table: `api/`, `web/`, `deploy/`, `docs/`, each with one line on what it holds.
  - The four API projects, each with a file-by-file table (file, responsibility) for Domain, Application, Infrastructure, and Api.
  - The dependency rule, drawn as the inward arrows, with an explanation of why the Domain has no dependencies and what that buys you (rules tested in isolation, rules that cannot be bypassed).
  - The data model diagram and the table mapping (entity to table, money to numeric(18,2)).
  - The request lifecycle for `POST /invoices`, traced hop by hop: routing, token auth middleware, validation, handler, domain entity building line items and enforcing the draft rule, EF Core insert, Postgres, response mapping, and the span created at each hop.
  - The telemetry flow: where OTel is configured, what is auto-instrumented versus manual, and how it reaches SigNoz on 4317.
  - Config and env vars: a table of every variable (connection string, Auth token, Otel endpoint, CORS origins) and where each is read.
  - The frontend map: routes to pages to the API client, briefly.
  - Ports table (API 5080, Vite 5173, Postgres 5432, SigNoz 3301).

- [ ] **Step 2: Read it back for accuracy**

Confirm every file referenced in the doc exists at the stated path and every port and variable matches the code. Fix any mismatch.

- [ ] **Step 3: Commit**

```bash
git add docs/ARCHITECTURE.md
git commit -m "docs: add architecture guided tour"
```

### Task 8.2: Write the README

**Files:**
- Create: `README.md`
- Create: `.env.example`
- Create: `.editorconfig`

**Interfaces:**
- Produces: the top-level run guide, an env example, and shared formatting rules.

- [ ] **Step 1: Write the .editorconfig**

Create `.editorconfig`:
```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
indent_style = space
trim_trailing_whitespace = true

[*.{cs}]
indent_size = 4
csharp_style_namespace_declarations = file_scoped:warning

[*.{ts,tsx,js,jsx,json,css,html}]
indent_size = 2
```

- [ ] **Step 2: Write the root .env.example**

Create `.env.example`:
```
# API
CONNECTIONSTRINGS__APPDB=Host=localhost;Port=5432;Database=seesharp;Username=seesharp;Password=seesharp
AUTH__TOKEN=dev-secret-token
OTEL__ENDPOINT=http://localhost:4317

# Web
VITE_API_TOKEN=dev-secret-token
```

- [ ] **Step 3: Write the README**

Create `README.md` in a plain human voice, no em dashes, covering:
  - What this project is and why it exists (a learning reference).
  - Prerequisites and how to install them on macOS: `brew install --cask dotnet-sdk` (.NET 10), `brew install node`, Docker Desktop. State the verify commands.
  - The layout in one short tree.
  - Quick start (SDK-first): start Postgres (`docker compose -f deploy/docker-compose.yml up -d postgres`), run the API (`dotnet run --project api/SeeSharp.Api`), run the web (`cd web && npm install && npm run dev`), and the auth token to use.
  - How to see traces: start SigNoz (`docker compose -f deploy/signoz/docker-compose.yml up -d`), generate traffic, open the SigNoz UI, find the `see-sharp-api` service.
  - The fully containerized path: `docker compose -f deploy/docker-compose.yml up --build`.
  - Running tests: `dotnet test` from `api/` (needs Docker for Testcontainers).
  - The ports table.
  - A pointer to `docs/ARCHITECTURE.md` for the guided tour.

- [ ] **Step 4: Final full verification**

Run the whole thing from a clean state: Postgres up, `dotnet test` from `api/` green, API runs and seeds, web builds and loads, SigNoz receives traces. Note any step that does not work and fix it before finishing.

- [ ] **Step 5: Commit**

```bash
git add README.md .env.example .editorconfig
git commit -m "docs: add readme, env example, and editorconfig"
```

---

## Self-Review Notes

Spec coverage check against the design doc:
- Domain isolation with rich entities and private setters: Phase 1.
- Clean-ish four projects, inward dependencies, no MediatR/AutoMapper/repositories: Phases 0 to 3.
- Money as decimal and numeric(18,2): Money value object (1.1), EF config (3.1).
- Full endpoint surface (Clients, Invoices with status change, Expenses, Categories, Reports): Phase 4.
- DTOs as records, FluentValidation, ProblemDetails, pagination, hardcoded single-account auth: Phase 4.
- EF Core migrations and seed data: Phase 3.
- OpenTelemetry traces, metrics, logs to SigNoz, service name see-sharp-api, non-blocking when down, manual `invoice.status_change` span (Task 5.1 Step 5) and custom `invoices_created` metric: Phase 5.
- SigNoz self-hosted in Docker: Task 6.1.
- Dockerfiles and full compose: Task 6.2.
- React Vite TS with React Router, typed API client, four pages: Phase 7.
- Testing: domain unit tests (Phase 1), integration tests with Testcontainers (4.5).
- Docs: ARCHITECTURE.md guided tour and README: Phase 8.
- .NET 10, nullable, warnings as errors, .editorconfig: Task 0.2 and 8.2.

Gap fixed inline: the manual `invoice.status_change` span the spec calls for is now a concrete step (Task 5.1 Step 5) with a shared `AppTelemetry.ActivitySource` registered in the tracer via `AddSource`, not an optional extra. This makes both telemetry paths (auto-instrumentation and hand-rolled spans) part of the deliverable.
