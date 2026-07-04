# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**WIMS** (نظام إدارة المخازن والمخزون) — warehouse & inventory management for وزارة العدل (Ministry of Justice). .NET 8, Clean Architecture + CQRS. Implementation follows [`plan.md`](plan.md) across 7 phases. Phase 0 (auth/RBAC/audit) and the core of Phase 1 (catalog + Excel import) are complete; remaining Phase 1 = opening-balance import + employee import.

Domain names, user-facing messages, and validation text are **Arabic**; code identifiers are English. Keep this convention when adding features.

## Commands

```bash
# Build the whole solution
dotnet build WIMS.slnx

# Run the API (auto-seeds base data on startup; Swagger at /swagger in Development)
dotnet run --project src/WIMS.WebApi

# Apply migrations / create the database
dotnet ef database update -p src/WIMS.Infrastructure -s src/WIMS.WebApi

# Add a migration (StartupProject flag is required — see DesignTimeDbContextFactory)
dotnet ef migrations add <Name> -p src/WIMS.Infrastructure -s src/WIMS.WebApi

# Run all tests (xUnit)
dotnet test

# Run one test project
dotnet test tests/WIMS.Application.Tests
# Run a single test by name
dotnet test --filter "FullyQualifiedName~SimpleMapperTests"
```

DB connection is `(localdb)\MSSQLLocalDB`, database `WIMS`. Default seeded admin: **`admin` / `Admin@12345`** (change in production).

## Environment note (non-obvious)

The dev machine has **only .NET SDK 10 installed, no SDK 8**. `net8.0` targeting is pinned centrally in [`Directory.Build.props`](Directory.Build.props) and works because the 8.0 runtime/ref packs are present. Consequences:
- The solution is the new XML `.slnx` format (`WIMS.slnx`), not `.sln` — SDK 10 generates `.slnx`.
- The SDK 10 WebApi template injects `Microsoft.AspNetCore.OpenApi` (net10); this project uses **Swashbuckle** instead for Swagger.

## Architecture

Dependency rule: `WebApi → Infrastructure → Application → Domain`, with `Shared` referenced across layers.

```
src/
├── WIMS.Domain/          # Entities, enums, no dependencies
├── WIMS.Application/      # CQRS handlers, MediatR behaviors, interfaces, DTOs
├── WIMS.Infrastructure/   # EF Core, Identity, JWT, external services
├── WIMS.WebApi/          # Controllers, middleware, DI, auth policies
└── WIMS.Shared/          # Result<T>, Error, Guard, SimpleMapper
```

### CQRS + MediatR pipeline

- Commands implement `ICommand<T>` (marker `IBaseCommand`); queries implement `IQuery<T>`. See [ICommand.cs](src/WIMS.Application/Common/Messaging/ICommand.cs). The marker interface is what drives auditing — **queries are never audited**.
- Pipeline behaviors run in order **Validation → Logging → Audit** (registered in [DependencyInjection.cs](src/WIMS.Application/DependencyInjection.cs)).
- A feature = a folder under `Features/<Area>/<Action>/` with Command/Query + Handler + Validator. Small reference-data features (Units, Warehouses) are consolidated into a single `*Features.cs` file — follow the nearest existing pattern.
- Handlers return `Result<T>` / `Result` (from `WIMS.Shared.Results`). Return `Error.NotFound/Validation/Conflict/...` for expected failures — **do not throw** for business-rule violations.

### Result → HTTP mapping

Controllers are thin: inject `ISender`, send the request, call `.ToActionResult()` from [ApiResults.cs](src/WIMS.WebApi/Common/ApiResults.cs), which maps `ErrorType` to status codes (Validation→400, Unauthorized→401, Forbidden→403, NotFound→404, Conflict→409). Queries returning plain data use `Ok(...)` directly.

### Auth & RBAC

- `AddIdentityCore` (no cookies) + JWT Bearer. No ASP.NET Identity UI.
- Fine-grained permissions via `permission`-type claims and a **dynamic policy provider** ([PermissionPolicyProvider.cs](src/WIMS.WebApi/Authorization/PermissionPolicyProvider.cs)). `[Authorize(Policy = PermissionKeys.Items.Manage)]` works without pre-registering each policy.
- **All permission keys live in [PermissionKeys.cs](src/WIMS.Domain/Authorization/PermissionKeys.cs)** — the single source of truth used by both seeding and `[Authorize]`. Add new permissions there and nowhere else.

### Persistence & auditing

- [AppDbContext](src/WIMS.Infrastructure/Persistence/AppDbContext.cs) merges Identity tables with domain entities. Its `SaveChangesAsync` override auto-stamps `CreatedAt/By` & `ModifiedAt/By` on `IAuditableEntity`, and converts deletes of `BaseEntity` into **soft deletes** (`IsDeleted = true`). Never hard-delete a `BaseEntity`.
- Business-level audit trail (who did which command) is written automatically by `AuditBehavior` to `AuditLogs` — no manual audit code in handlers.
- EF configurations are auto-applied from the Infrastructure assembly (`ApplyConfigurationsFromAssembly`); add a `*Configuration.cs` under `Persistence/Configurations/` for each new entity.

### Conventions worth matching

- **SimpleMapper** ([WIMS.Shared/Mapping](src/WIMS.Shared/Mapping/)) is a lightweight in-house reflection+cache mapper used instead of AutoMapper. Prefer explicit `Select(...)` projections in query handlers (see WarehouseFeatures.cs); use SimpleMapper only where a general map helps.
- Excel import (items, opening balances) uses **ClosedXML** via `IExcelReader`; imports do row + reference validation, are atomic, normalize Arabic-Indic digits, and return an error report. See [Features/Import/](src/WIMS.Application/Features/Import/).
- Item codes follow the `GG-CCCC-SSSS` format generated by [ItemCodeGenerator](src/WIMS.Infrastructure/Services/ItemCodeGenerator.cs).
- Integration tests use `WebApplicationFactory` — `Program` is `public partial` for that reason.

## Domain guidance

Phase 1 was designed against 11 business rules (BR-01..11) and 14 risk guards (R-01..14) documented in `plan.md`. When a task touches inventory semantics (stock, balances, WAC, tracking dimensions), consult `plan.md` and preserve the referenced BR/R numbers cited in code comments.

## Versioning

The repo is a git repository with tagged releases and a maintained [`CHANGELOG.md`](CHANGELOG.md) (Keep a Changelog format). Workflow:

- Ongoing work is logged under the `[غير مُصدَر]` (Unreleased) section of `CHANGELOG.md` as it happens.
- When the user announces a version number (e.g. "خلّيها 1.1.0"), Claude should:
  1. Diff the working tree against the previous tag (`git diff <prev-tag>..HEAD --stat`, plus reading the actual changes) to compile an accurate list of what changed — don't just rely on conversation memory once a previous tag exists.
  2. Move the `Unreleased` content into a new `## [X.Y.Z] — YYYY-MM-DD` section in `CHANGELOG.md`, reset `Unreleased` to empty.
  3. Bump `<Version>` in [`Directory.Build.props`](Directory.Build.props) and `"version"` in [`wims-client/package.json`](wims-client/package.json) to match.
  4. `git add -A && git commit -m "chore(release): vX.Y.Z"` then `git tag vX.Y.Z`.
- The `0.1.0` baseline tag/entry predates this workflow and was written from conversation history, not a git diff — treat it as a one-time exception.

**Don't wait to be asked — ask.** If the user is clearly preparing/finalizing a release without stating a version number, proactively ask for one rather than silently skipping CHANGELOG/tagging. Signals this is happening: running or asking to run `build-package.ps1` / `publish-monsterasp.ps1` (or an equivalent publish/build-for-delivery step), saying things like "جهّزها للتسليم", "خلاص جاهزة نرفعها/نسلّمها", "ابني الحزمة النهائية", or otherwise packaging output meant to leave this machine. When you see one of these, ask something like "قبل ما نجهّز الحزمة — إيه رقم الإصدار؟" before finishing that turn. Don't guess a number yourself and don't silently proceed without one — the user may genuinely forget, that's exactly the case this rule exists for.
