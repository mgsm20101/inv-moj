# Changelog

This file documents all notable changes to **WIMS** (Warehouse & Inventory Management System — Ministry of Justice), release by release.

Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and [Semantic Versioning](https://semver.org/) (MAJOR.MINOR.PATCH).

## How this works

- Ongoing work is logged first under the **[Unreleased]** section as it happens.
- When you announce a version number (e.g. "call it 1.1.0"), the **[Unreleased]** content is moved into a new section titled with the version and date, a **git tag** is created with the same number, and the version is bumped in `Directory.Build.props` and `wims-client/package.json`.
- Starting from the first tagged release, every subsequent release's details are extracted automatically by diffing (`git diff`) the previous tag against the current state — to keep the log accurate.

---

## [Unreleased]

### Fixed
- `SecurityHeadersMiddleware`: the Content-Security-Policy header was `default-src 'none'`, written back when the API served JSON only. Once the Angular SPA started being served from the same origin (`wwwroot`), this silently blocked every script, stylesheet, and image on the live MonsterASP deployment — the site loaded `index.html` but rendered a blank page. Relaxed to `default-src 'self'` (+ `style-src 'unsafe-inline'` for Angular's inlined critical CSS), still same-origin-only with no external CDNs allowed.
- `wims-client/angular.json`: production builds render broken/unstyled UI (giant unstyled SVG icons, e.g. the login button) even after the CSP fix above. Root cause: Angular's default critical-CSS optimization injects the real stylesheet as `<link ... media="print" onload="this.media='all'">` — an inline event handler, which `script-src 'self'` (correctly) blocks, so the stylesheet's `media` never flips to `all` and the whole external stylesheet (all component styles, e.g. `.btn__icon`) never actually applies on screen. Disabled `inlineCritical` in the `styles` optimization option instead of loosening the CSP further — verified locally that the built `index.html` no longer contains any `onload` attribute and `.btn__icon` now only exists in the plain, normally-loaded stylesheet.
- `wims-client/angular.json`: login (and every other API call) failed on the live site with a CSP `connect-src 'self'` violation trying to reach `http://localhost:5021/api/...` — the production build was still using the dev `environment.ts` (`apiBaseUrl: 'http://localhost:5021/api'`) instead of `environment.prod.ts` (`apiBaseUrl: '/api'`), because the `production` build configuration never had a `fileReplacements` entry at all. Added it. Verified locally: `localhost:5021` no longer appears anywhere in the built output.

### Added
- `src/WIMS.WebApi/Properties/PublishProfiles/MonsterASP.pubxml`: WebDeploy publish profile (non-secret data only) + a new section in `DEPLOY-MonsterASP-AR.md` for direct command-line deployment via `dotnet publish /p:DeployOnBuild=true`.
- `.gitignore`: excluded `*.publishSettings` (contains a real WebDeploy password when downloaded from the hosting control panel).
- `appsettings.Production.json`: the real MonsterASP MSSQL connection string (without the password) and `AllowedHosts` restricted to the actual domain `invmoj.runasp.net` — automatically included in every future MonsterASP package; only the password needs to be filled in on the server.
- `.github/workflows/deploy-monsterasp.yml`: optional automated deployment (manual `workflow_dispatch` trigger) to MonsterASP via GitHub Actions — builds Angular, runs tests, then deploys via `rasmusbuchholdt/simply-web-deploy` (instead of MSBuild's broken built-in WebDeploy). Requires 4 GitHub secrets the user adds themselves (documented in `DEPLOY-MonsterASP-AR.md`).

### Fixed (documentation)
- `DEPLOY-MonsterASP-AR.md`: flagged the "WebDeploy from the command line" method as currently broken — it reproducibly throws `MSB4006: circular dependency` (verified firsthand on .NET SDK 10.0.202; tried `dotnet msbuild -t:Publish` as a workaround and it didn't fix it either). FTP/File Manager is now the recommended method instead.

### Fixed
- `.github/workflows/deploy-monsterasp.yml`: the publish step failed in CI with `NETSDK1047: ... doesn't have a target for 'net8.0/win-x86'` — caused by `--no-restore` on a step that needs a win-x86-specific restore the earlier RID-less `dotnet restore` step never produced. Removed `--no-restore` from that step.
- `.github/workflows/deploy-monsterasp.yml`: pinning the job to SDK 8.0.x (an earlier fix, to dodge the SDK-10 `MSB4006` bug) then broke restore/build/test with `MSB4068: The element <Solution> is unrecognized` — SDK 8 cannot parse the `WIMS.slnx` XML solution format at all. Reverted: the job now uses SDK 10.x explicitly via `actions/setup-dotnet` (no `global.json` pin). This is safe because the `MSB4006` bug is specific to `DeployOnBuild=true`/WebDeploy MSBuild integration, which this workflow never uses (it publishes plainly and deploys via a separate action) — so there was never actually a need to avoid SDK 10 here.

### Other
- `.gitignore`: excluded all Markdown files (`*.md`) from the remote repository except `README.md` and `CHANGELOG.md` (per user request) — `CLAUDE.md` and the planning/deployment docs are now local-only (no longer tracked in Git).
- `README.md`: fully rewritten to reflect the project's actual current state (it was previously stuck describing the old Phase 0-1 status).
- `CHANGELOG.md` and `README.md` translated to English per user request (the rest of the project's user-facing content — domain names, validation messages — stays Arabic per convention; these two meta-documentation files are the exception).

---

## [1.0.0.0] — 2026-07-04

**First official release, announced by the user.** At the user's request, this section consolidates everything built in WIMS so far into a single comprehensive log (merging the internal baseline point 0.1.0 with everything that came after it).

### Added
- **Operational foundation (Phases 0-6):** item catalog, transaction vouchers (receipt/issue/transfer/adjustment/return), role-routed multi-step approval engine, Excel import (items/opening balances), physical stock counts and their records, stock alerts, reports (view + PDF/Excel export), main dashboard.
- **Full operational RBAC system:** CQRS for managing users/roles/permissions, complete admin UI (users and roles tabs + permission matrix), self-service password change, dynamic authorization routing (`PermissionPolicyProvider`), fine-grained seeded roles (System Admin/Warehouse Keeper/Approver/Finance Manager/Auditor) instead of one all-powerful role, approval routing based on the user's actual role.
- **Permission-based UI element hiding:** structural directive `*appHasPermission` + route guards.
- **Custody:** employee custody statement, custody clearance, and a brand-new **individual custody item return** feature — automatically creates an auto-approved return voucher and actually restores the stock balance.
- **Self-contained packaging & delivery (SelfHost):** self-contained single-file publish for a single Windows machine with SQL Server Express, frontend served from the same origin as the API, self-hosted Arabic fonts (offline), `build-package.ps1` / `setup.ps1` / `Start-WIMS.bat` scripts, Arabic user guide (`README-User-AR.md`).
- **MonsterASP.NET deployment package:** `publish-monsterasp.ps1` (framework-dependent win-x86 publish), `appsettings.Production.json` configured for MonsterASP's connection string format, and a step-by-step deployment guide (`DEPLOY-MonsterASP-AR.md`).
- **User training materials:** an interactive illustrated user guide (HTML) + a printable Word version, an Egyptian-Arabic-dialect video recording script, and an interactive video studio (HTML) that automatically produces a real video file (MP4/WebM) with either an automated voice or the user's own voice.
- **The versioning system itself:** Git + `CHANGELOG.md` (Keep a Changelog) + version tags, plus a proactive rule to ask for a version number when preparing any delivery package even if not explicitly requested (documented in the Versioning section of `CLAUDE.md`).
- **An approved policy decision:** removed the "you may not approve a voucher you created yourself" restriction for all voucher types with no exception (an intentional change, documented in `plan.md`).

### Fixed
- **`AuditBehavior`:** used to write an audit log entry even when the command actually failed; it now only logs on genuine success.
- **`CustodyProvisioningService`:** adding a custody item via `parent.Items.Add(...)` was incorrectly classified as `Modified` instead of `Added` (a common EF Core pitfall), causing a `DbUpdateConcurrencyException`; replaced with an explicit `db.CustodyItems.Add(...)`.
- **NG0600 (Angular):** the `*appHasPermission` directive running inside an `effect()` without `allowSignalWrites` was silently dropping some buttons (like the approve button) when combined with a `viewChild.required()` query in the same component.
- **`appsettings.Production.json`:** referenced a Serilog sink named `MSSqlServer` that wasn't even installed as a NuGet package in the project — this would have crashed on startup immediately on any real Production deployment; replaced with a file-based sink that matches the packages actually installed.
- Intermittent `RateLimitingTests` failures caused by poisoning the shared test database via a real Identity lockout on the `admin` user.

### Versioning note
- The .NET versioning format (`Directory.Build.props`) uses **four segments** (Major.Minor.Build.Revision, e.g. `1.0.0.0`) following the .NET `AssemblyVersion`/`FileVersion` convention — per the user's explicit request. `wims-client/package.json`, on the other hand, uses standard **three-segment Semantic Versioning** (`1.0.0`, no fourth number) because npm rejects a four-segment string as a valid version; the two numbers are treated as synonyms for the same logical release.

---

## [0.1.0] — Internal baseline (before the official announcement) — 2026-07-04

An internal baseline point created while setting up the versioning system itself, not a release the user announced. Its entire content is merged into **[1.0.0.0]** above as one comprehensive consolidated log — kept here only for historical accuracy alongside the `v0.1.0` Git tag.
