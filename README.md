# WIMS — Warehouse & Inventory Management System

A warehouse and inventory management system for the Ministry of Justice, built with **.NET 8** (Clean Architecture + CQRS/MediatR) and **Angular 18** on the frontend, with a fully operational role-based access control (RBAC) system.

## Current Status

Core phases (0-6) are complete and tested:

- **Item catalog** + Excel import (items/opening balances/employees)
- **Transaction vouchers**: receipt, issue, transfer, adjustment, return — with a role-routed multi-step approval engine
- **Custody**: issuing durable items to employees, custody statements, individual item return to stock, custody clearance
- **Physical stock counts** and their records, plus stock alerts (min level/expiry/stagnation)
- **Reports**: view and export as PDF/Excel
- **Full RBAC system**: user/role/permission management, permission matrix, self-service password change, permission-based UI element hiding
- **Packaging & delivery**: a self-contained package for a single Windows machine with SQL Server Express, plus support for deploying to shared IIS hosting

See the full detailed change history in the local `CHANGELOG.md`.

## Requirements

- .NET SDK 8+ (also works on SDK 9/10, since `net8.0` is pinned centrally via `Directory.Build.props`)
- Node.js 20+ and npm (for the Angular frontend)
- SQL Server LocalDB or SQL Server 2019/2022 (or SQL Server Express for self-hosted deployment)
- `dotnet-ef` (`dotnet tool install -g dotnet-ef`)

## Running locally

```bash
# 1) Apply migrations and create the database
dotnet ef database update -p src/WIMS.Infrastructure -s src/WIMS.WebApi

# 2) Run the API (seeds base data automatically on startup)
dotnet run --project src/WIMS.WebApi

# 3) Run the Angular frontend (in a separate terminal)
cd wims-client && npm install && npm start
```

- API: `http://localhost:5021` (Swagger at `/swagger`, Development environment only)
- Frontend: `http://localhost:4200`
- Default user: **`admin` / `Admin@12345`** — ⚠️ must be changed immediately after first login.

## Smoke Test

```bash
curl -X POST http://localhost:5021/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@12345"}'

curl http://localhost:5021/api/me -H "Authorization: Bearer <TOKEN>"
```

## Architecture

```
src/
├── WIMS.Domain/          # Entities and rules (no dependencies)
├── WIMS.Application/     # CQRS + behaviors + interfaces
├── WIMS.Infrastructure/  # EF Core + Identity + JWT + services
├── WIMS.WebApi/          # Controllers + middleware + DI
└── WIMS.Shared/          # Result<T> + Guard + SimpleMapper
wims-client/              # Angular 18 frontend (standalone components)
tests/                    # Domain / Application / Integration
```

Dependency rule: `WebApi → Infrastructure → Application → Domain`.

## Tests

```bash
dotnet test
```

## Packaging & Deployment

- **Self-contained package (single machine + SQL Express):** `.\build-package.ps1`
- **Shared IIS hosting:** `.\publish-monsterasp.ps1` (+ an optional GitHub Actions workflow under `.github/workflows/`)

## Technical Notes

- **SimpleMapper**: a lightweight in-house mapping tool in `WIMS.Shared`, used instead of AutoMapper.
- **Auditing**: automatically written for every command (`ICommand`) via `AuditBehavior` — no manual audit code.
- **RBAC**: fine-grained permissions via `permission`-type claims and a dynamic policy provider.
- The solution uses the `WIMS.slnx` format (the new XML-based solution format).
