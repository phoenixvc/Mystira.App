# Mystira Application Suite

![.NET 9](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)
![Azure Cosmos DB](https://img.shields.io/badge/Azure-Cosmos%20DB-0089D6?logo=microsoftazure&logoColor=white)
![Blazor PWA](https://img.shields.io/badge/Client-Blazor%20PWA-5C2D91?logo=blazor&logoColor=white)
![CI Ready](https://img.shields.io/badge/CI-GitHub%20Actions-2088FF?logo=githubactions&logoColor=white)
![Repo Type](https://img.shields.io/badge/Repo-Monorepo-6f42c1?logo=github&logoColor=white)
![Quality Gates](https://img.shields.io/badge/Tests-dotnet%20test,%20dotnet%20format,%20npm%20run%20lint-20C997?logo=github&logoColor=white)

The Mystira repository hosts the full suite of services, libraries, and client applications that power the Mystira experience. It includes backend APIs, domain and infrastructure libraries, the Cosmos-analytical console tool, and the Blazor PWA front-end—all sharing a cohesive domain model and now standardised on .NET 9.

> **Why this matters:** Everything in the repo builds against the same runtime, which simplifies dependency management, improves security posture, and keeps developer tooling consistent.

## Contents

- [Mystira Application Suite](#mystira-application-suite)
  - [Contents](#contents)
  - [Repository Overview](#repository-overview)
  - [Technology Stack](#technology-stack)
  - [Getting Started](#getting-started)
    - [Prerequisites](#prerequisites)
    - [Build](#build)
    - [Run Key Projects](#run-key-projects)
  - [Upgrade Verification Checklist](#upgrade-verification-checklist)
  - [Project Analysis](#project-analysis)
    - [Strengths](#strengths)
    - [Risks \& Gaps](#risks--gaps)
    - [Opportunities](#opportunities)
  - [Recommendations](#recommendations)
  - [Testing \& Quality Gates](#testing--quality-gates)
  - [Contributing / PR Checklist](#contributing--pr-checklist)
  - [Developer Quality of Life](#developer-quality-of-life)
  - [Further Reading](#further-reading)

## Repository Overview

| Label         | Area                                   | Description                                                                             |
| ------------- | -------------------------------------- | --------------------------------------------------------------------------------------- |
| 🧠 Domain      | `src/Mystira.App.Domain`               | Core domain models, enumerations, and shared business logic reused across every layer.  |
| ☁️ Azure Infra | `src/Mystira.App.Infrastructure.Azure` | Azure-specific configuration objects plus Cosmos DB & Blob Storage health checks.       |
| 🌐 Public API  | `src/Mystira.App.Api`                  | ASP.NET Core API serving Mystira clients on top of Cosmos DB.                           |
| 🛡️ Admin API   | `src/Mystira.App.Admin.Api`            | Internal-facing API surface for moderation, content workflows, and tooling.             |
| 📱 PWA         | `src/Mystira.App.PWA`                  | Blazor WebAssembly PWA with offline assets, IndexedDB sync, audio helpers, and haptics. |
| 📊 Ops Console | `Mystira.App.CosmosConsole`            | Command-line utility for Cosmos DB exports, stats, and operational insights.            |

## Technology Stack

- **Languages & Runtimes:** C# / ASP.NET Core on .NET 9 across APIs, console, and PWA host.
- **Data Layer:** Azure Cosmos DB (EF Core provider) and Azure Blob Storage for binary assets.
- **Client Enhancements:** Service workers, IndexedDB caching, audio/haptics JS interop, and dice utilities.
- **Tooling:** CsvHelper (exports), System.CommandLine, Microsoft.Extensions.* configuration/logging, Azure health checks.

## Getting Started

### Prerequisites

- .NET 9 SDK (`dotnet --list-sdks` should show 9.x).
- Node.js 18+ for PWA build tooling/service-worker bundling.
- Azure resources (Cosmos DB account, Blob Storage) or emulators.
- Repository secrets (connection strings, credentials) supplied via User Secrets, environment variables, or Azure Key Vault.

### Build

```bash
dotnet build Mystira.sln
```

### Run Key Projects

```bash
# Public API
dotnet run --project src/Mystira.App.Api/Mystira.App.Api.csproj

# Admin API
dotnet run --project src/Mystira.App.Admin.Api/Mystira.App.Admin.Api.csproj

# Cosmos console exports
dotnet run --project src/Mystira.App.CosmosConsole/Mystira.App.CosmosConsole.csproj -- export --output sessions.csv

# Blazor PWA host
dotnet run --project src/Mystira.App.PWA/Mystira.App.PWA.csproj
```

Configure `appsettings.Development.json`, user secrets, or environment variables with Cosmos and Blob credentials before running services.

## Upgrade Verification Checklist

| Project File                                                 | Target Framework | Notes                                                                          |
| ------------------------------------------------------------ | ---------------- | ------------------------------------------------------------------------------ |
| `src/Mystira.App.Api/Mystira.App.Api.csproj`                 | `net9.0`         | Public API upgraded to .NET 9 for C# 12 features and ASP.NET Core perf.        |
| `src/Mystira.App.Admin.Api/Mystira.App.Admin.Api.csproj`     | `net9.0`         | Admin API matches the public surface to avoid dependency drift.                |
| `src/Mystira.App.PWA/Mystira.App.PWA.csproj`                 | `net9.0`         | Blazor host upgraded; WebAssembly assets continue to run on latest runtime.    |
| `Mystira.App.CosmosConsole/Mystira.App.CosmosConsole.csproj` | `net9.0`         | Operational tooling aligned so it benefits from the same SDK/tooling pipeline. |

> **Packages refreshed:** Blazor WebAssembly client libraries (`Microsoft.AspNetCore.Components.WebAssembly`, DevServer, `Microsoft.Extensions.Http`, `System.Text.Json`) now target version 9.0.0 to match the runtime upgrade.
 **Tip:** If you upgrade additional projects, run `dotnet workload update` to keep WebAssembly and MAUI workloads in sync with the 9.0 SDK.

## Project Analysis

### Strengths

- **Shared Domain Contracts:** Centralised models (`ClassificationTag`, `Modifier`, `Character`, etc.) keep APIs, console, and PWA aligned.
- **Operational Tooling:** Cosmos console exports plus Azure health checks provide observability and data-access workflows.
- **Offline-first Client:** IndexedDB caching, service workers, audio, dice haptics, and other device integrations deliver a richer PWA experience.

### Risks & Gaps

- **Configuration Duplication:** APIs and console each define Cosmos/Blob configuration blocks, risking drift.
- **PII Handling:** Multiple components expose user PII (emails, aliases) without documented redaction/logging standards.
- **Documentation Coverage:** Service-specific runbooks and environment guides are still sparse despite the new high-level README.

### Opportunities

- **Consolidated Configuration Package:** Extract shared options (CosmosDbOptions, BlobStorageOptions, email settings) into a reusable assembly.
- **Automated Exports:** Enhance the console with date/scenario filters, scheduled runs, and automatic Blob uploads or Power BI triggers.
- **Testing & Validation:** Add contract/integration tests for EF converters (classification tags, modifiers), IndexedDB abstractions, and Azure health checks.
- **Security Posture:** Document Key Vault integration, standardise Managed Identity/Azure AD usage, and highlight PII-safe logging practices.
- **Front-end Resilience:** Strengthen service-worker caching and IndexedDB migrations to improve offline robustness and release rollouts.

## Recommendations

1. **Unify Configuration & Secrets Management:** Ship a shared configuration package plus deployment guidance so every service consumes Cosmos/Blob/email credentials consistently (ideally via Key Vault or Managed Identity).
2. **Document Service Runbooks:** Add `/docs` pages or per-project READMEs covering environment variables, local-debug steps, and smoke tests for App API, Admin API, and PWA.
3. **Expand Automated Reporting:** Extend the console tool with filterable exports, scheduling hooks, and optional PII masking to integrate into analytics pipelines.
4. **PII Governance:** Define redaction rules for logs/CSV exports, establish handling guidance (storage duration, secure transfer), and automate masking where possible.
5. **Quality Gates:** Introduce CI-backed integration tests for shared domain conversions, Azure health checks, and PWA storage helpers to catch regressions early.

## Testing & Quality Gates

| Stage                    | Command                                                                                                         | Purpose                                                  |
| ------------------------ | --------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------- |
| Unit / Integration Tests | `dotnet test Mystira.sln`                                                                                       | Runs cross-project tests (APIs, domain, infrastructure). |
| Formatting               | `dotnet format Mystira.sln`                                                                                     | Keeps C# style consistent before pushing a PR.           |
| PWA Lint / Build         | `npm install` (once), `npm run lint` / `npm run build` (inside `src/Mystira.App.PWA` if JS assets are modified) | Ensures JS/service-worker assets remain valid.           |
| Console Smoke Test       | `dotnet run --project Mystira.App.CosmosConsole/... -- stats`                                                   | Confirms Cosmos CLI still connects post-change.          |

Wire these into CI (GitHub Actions/Azure DevOps) to block merges when quality gates fail.

## Contributing / PR Checklist

1. **Create a feature branch** off `main`.
2. **Update code + tests**, keeping target frameworks at `net9.0`.
3. **Run quality gates** listed above.
4. **Update documentation** (README or `/docs/*`) if behaviour/config changes.
5. **Open a PR** describing:
   - Motivation and scope.
   - Testing performed (commands + outcomes).
   - Any config/secret implications or follow-up tasks.
6. **Request review** from at least one API maintainer and one client-side maintainer when changes cross boundaries.

## Developer Quality of Life

- **Dev Containers / Codespaces:** Base images should include the .NET 9 SDK, Node.js 18, and Azure CLI for parity with local builds.
- **CI Hooks:** Ensure GitHub Actions (or equivalent) build the solution, run unit/integration tests, and execute the console tool’s smoke commands.
- **Observability:** Leverage the existing health-check endpoints in deployment manifests and surface them in dashboards/alerts.

## Further Reading

- `docs/NEXT_ITERATION_PLAN.md` – roadmap context and future iteration ideas.
- `src/*/Validation/ScenarioSchemaDefinitions.cs` – schema enforcement shared across services.
- `src/Mystira.App.Infrastructure.Azure/HealthChecks` – Cosmos/Blob readiness probes used by the APIs.
