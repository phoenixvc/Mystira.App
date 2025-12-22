# Admin API Extraction Migration Plan

**Status**: Proposed
**Date**: 2025-12-22
**Owner**: Development Team
**Related ADRs**: [ADR-0005](../architecture/adr/ADR-0005-separate-api-and-admin-api.md)

---

## Executive Summary

This plan outlines the migration strategy for extracting the Admin API from `Mystira.App` into separate repositories. The extraction creates cleaner separation of concerns, enables independent release cycles, and improves security isolation.

---

## Target State

### New Repositories

| Repository | Description | Tech Stack |
|------------|-------------|------------|
| `Mystira.Admin.Api` | Admin backend API (REST/gRPC) | .NET 9 |
| `Mystira.Admin.UI` | Admin frontend (modern SPA) | React/Blazor |

### Shared Packages (NuGet)

| Package | Description |
|---------|-------------|
| `Mystira.App.Domain` | Domain models and entities |
| `Mystira.App.Application` | Application services, CQRS handlers |
| `Mystira.App.Infrastructure.Cosmos` | Cosmos DB infrastructure |
| `Mystira.App.Infrastructure.Auth` | Authentication infrastructure |
| `Mystira.App.Contracts` | Shared DTOs and contracts |

---

## Phase 1: Setup Shared NuGet Packages

### 1.1 Create Internal NuGet Feed

**Platform**: Azure DevOps Artifacts (or GitHub Packages)

```yaml
# azure-pipelines.yml - NuGet publish workflow
trigger:
  branches:
    include:
      - main
  paths:
    include:
      - 'src/Mystira.App.Domain/**'
      - 'src/Mystira.App.Application/**'
      - 'src/Mystira.App.Infrastructure.*/**'

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '9.0.x'

  - task: NuGetAuthenticate@1

  - script: |
      dotnet pack src/Mystira.App.Domain -c Release -o $(Build.ArtifactStagingDirectory)
      dotnet pack src/Mystira.App.Application -c Release -o $(Build.ArtifactStagingDirectory)
      dotnet pack src/Mystira.App.Infrastructure.Cosmos -c Release -o $(Build.ArtifactStagingDirectory)
    displayName: 'Pack NuGet packages'

  - task: NuGetCommand@2
    inputs:
      command: 'push'
      packagesToPush: '$(Build.ArtifactStagingDirectory)/*.nupkg'
      nuGetFeedType: 'internal'
      publishVstsFeed: 'mystira-packages'
```

### 1.2 Package Versioning Strategy

```xml
<!-- Directory.Build.props -->
<PropertyGroup>
  <VersionPrefix>1.0.0</VersionPrefix>
  <VersionSuffix Condition="'$(Configuration)' == 'Debug'">preview</VersionSuffix>
</PropertyGroup>
```

### 1.3 Deliverables

- [ ] Azure DevOps Artifacts feed created
- [ ] Authentication configured for CI/CD
- [ ] Package publishing workflow tested
- [ ] Version management strategy documented

---

## Phase 2: Create Admin API Repository

### 2.1 Repository Setup

```bash
# Create repository
gh repo create phoenixvc/Mystira.Admin.Api --private

# Clone and setup
git clone https://github.com/phoenixvc/Mystira.Admin.Api.git
cd Mystira.Admin.Api

# Initialize .NET solution
dotnet new sln -n Mystira.Admin.Api
dotnet new webapi -n Mystira.Admin.Api -o src/Mystira.Admin.Api
dotnet sln add src/Mystira.Admin.Api
```

### 2.2 Project Structure

```
Mystira.Admin.Api/
├── src/
│   └── Mystira.Admin.Api/
│       ├── Controllers/
│       ├── Services/
│       ├── Configuration/
│       ├── Program.cs
│       └── Mystira.Admin.Api.csproj
├── tests/
│   └── Mystira.Admin.Api.Tests/
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── cd.yml
├── Mystira.Admin.Api.sln
└── README.md
```

### 2.3 Package References

```xml
<!-- Mystira.Admin.Api.csproj -->
<ItemGroup>
  <PackageReference Include="Mystira.App.Domain" Version="1.0.*" />
  <PackageReference Include="Mystira.App.Application" Version="1.0.*" />
  <PackageReference Include="Mystira.App.Infrastructure.Cosmos" Version="1.0.*" />
  <PackageReference Include="Mystira.App.Infrastructure.Auth" Version="1.0.*" />
</ItemGroup>
```

### 2.4 NuGet Configuration

```xml
<!-- nuget.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="mystira" value="https://pkgs.dev.azure.com/phoenixvc/_packaging/mystira-packages/nuget/v3/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <mystira>
      <add key="Username" value="azure" />
      <add key="ClearTextPassword" value="$(AZURE_ARTIFACTS_PAT)" />
    </mystira>
  </packageSourceCredentials>
</configuration>
```

### 2.5 Deliverables

- [ ] Repository created
- [ ] Solution structure established
- [ ] NuGet feed configured
- [ ] CI/CD workflows created

---

## Phase 3: Migrate Admin Controllers

### 3.1 Controllers to Migrate

| Controller | Endpoints | Priority |
|------------|-----------|----------|
| AccountsController | 5 | High |
| BadgeConfigurationsController | 4 | Medium |
| ContentBundlesController | 6 | Medium |
| ScenariosController (Admin) | 8 | High |
| UserProfilesController (Admin) | 4 | Medium |
| MediaController (Admin) | 3 | Low |

### 3.2 Migration Steps per Controller

1. Copy controller to new repository
2. Replace project references with package references
3. Update namespace to `Mystira.Admin.Api.Controllers`
4. Verify all CQRS handlers are available via packages
5. Add controller tests
6. Verify endpoints work

### 3.3 Example: AccountsController Migration

**Before (Mystira.App):**
```csharp
using Mystira.App.Application.CQRS.Accounts.Commands;
using Mystira.App.Application.CQRS.Accounts.Queries;

namespace Mystira.App.Admin.Api.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    // ...
}
```

**After (Mystira.Admin.Api):**
```csharp
using Mystira.App.Application.CQRS.Accounts.Commands;
using Mystira.App.Application.CQRS.Accounts.Queries;

namespace Mystira.Admin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]  // Note: removed /admin prefix
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;
    // ...
}
```

### 3.4 Deliverables

- [ ] All admin controllers migrated
- [ ] Unit tests passing
- [ ] Integration tests passing
- [ ] API documentation updated

---

## Phase 4: Extract Admin UI

### 4.1 Current State

Admin UI is currently Razor Pages embedded in `Mystira.App.Admin.Api`:
- `Views/` folder with Razor templates
- Server-side rendering
- Tightly coupled with API

### 4.2 Target State Options

**Option A: Keep Razor Pages (Minimal Change)**
- Move Razor Pages to new Admin.Api repository
- No frontend framework change
- Faster to implement

**Option B: Modern SPA (Recommended)**
- Create `Mystira.Admin.UI` repository
- Use React/Vue/Blazor standalone
- Better separation of concerns
- Independent deployment

### 4.3 Modern SPA Architecture

```
Mystira.Admin.UI/
├── src/
│   ├── components/
│   ├── pages/
│   ├── services/
│   │   └── api.ts  # Calls Admin API
│   ├── App.tsx
│   └── main.tsx
├── public/
├── package.json
├── vite.config.ts
└── README.md
```

### 4.4 API Integration

```typescript
// src/services/api.ts
const API_BASE = import.meta.env.VITE_ADMIN_API_URL;

export const adminApi = {
  accounts: {
    list: () => fetch(`${API_BASE}/api/accounts`),
    get: (id: string) => fetch(`${API_BASE}/api/accounts/${id}`),
    // ...
  },
  // ...
};
```

### 4.5 Deliverables

- [ ] UI framework selected
- [ ] Repository created
- [ ] All UI components migrated
- [ ] API integration tested
- [ ] Deployment configured

---

## Phase 5: CI/CD Pipeline Setup

### 5.1 Admin API CI/CD

```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Authenticate NuGet
        run: |
          dotnet nuget add source https://pkgs.dev.azure.com/phoenixvc/_packaging/mystira-packages/nuget/v3/index.json \
            --name mystira \
            --username azure \
            --password ${{ secrets.AZURE_ARTIFACTS_PAT }} \
            --store-password-in-clear-text

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Test
        run: dotnet test --no-build --configuration Release
```

### 5.2 Deployment Strategy

- **Dev**: Deploy on every push to `develop`
- **Staging**: Deploy on PR merge to `main`
- **Production**: Manual approval required

### 5.3 Deliverables

- [ ] CI workflow for Admin API
- [ ] CD workflow for Admin API
- [ ] CI workflow for Admin UI
- [ ] CD workflow for Admin UI
- [ ] Environment configurations

---

## Phase 6: Migration Execution

### 6.1 Pre-Migration Checklist

- [ ] All shared packages published and versioned
- [ ] New repositories created and configured
- [ ] CI/CD pipelines tested
- [ ] Rollback procedures documented
- [ ] Team trained on new architecture

### 6.2 Migration Day Steps

1. **Freeze Changes** (15 min)
   - No new commits to Mystira.App Admin API code
   - Communicate freeze to team

2. **Deploy Parallel** (30 min)
   - Deploy new Admin API alongside existing
   - Verify all endpoints work
   - Run smoke tests

3. **Traffic Switch** (15 min)
   - Update DNS/routing to new Admin API
   - Monitor for errors

4. **Verification** (30 min)
   - Test all admin functionality
   - Verify data consistency
   - Check logs for errors

5. **Go/No-Go Decision** (15 min)
   - If issues: execute rollback
   - If success: proceed to cleanup

### 6.3 Rollback Procedure

```bash
# If issues detected:
# 1. Switch traffic back to old Admin API
az network traffic-manager endpoint update \
  --name admin-new \
  --profile-name mystira-admin \
  --resource-group mystira-prod \
  --type azureEndpoints \
  --endpoint-status Disabled

az network traffic-manager endpoint update \
  --name admin-old \
  --profile-name mystira-admin \
  --resource-group mystira-prod \
  --type azureEndpoints \
  --endpoint-status Enabled

# 2. Investigate issues
# 3. Fix and retry migration
```

### 6.4 Deliverables

- [ ] Pre-migration checklist complete
- [ ] Migration executed successfully
- [ ] Verification passed
- [ ] Rollback tested (in staging)

---

## Phase 7: Cleanup

### 7.1 Code Removal

After successful migration (1-2 weeks of stability):

1. Remove Admin API controllers from Mystira.App
2. Remove Admin UI views from Mystira.App
3. Remove admin-specific dependencies
4. Update Mystira.App solution file

### 7.2 Infrastructure Cleanup

1. Decommission old Admin API infrastructure
2. Update DNS records
3. Archive old deployment configurations

### 7.3 Documentation Updates

1. Update Mystira.App README
2. Create Mystira.Admin.Api README
3. Create Mystira.Admin.UI README
4. Update architecture diagrams
5. Update ADRs with final decisions

### 7.4 Deliverables

- [ ] Old code removed
- [ ] Old infrastructure decommissioned
- [ ] Documentation updated
- [ ] Team notified of new architecture

---

## Success Criteria

| Criteria | Target | Measurement |
|----------|--------|-------------|
| All admin endpoints functional | 100% | API tests |
| No increase in error rate | < 0.1% | Monitoring |
| Build time improvement | < 10 min | CI metrics |
| Independent deployments | Yes | Deploy history |
| Security isolation verified | Yes | Security audit |

---

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Package version conflicts | Medium | High | Semantic versioning, testing |
| Data inconsistency | Low | Critical | Same database, rollback plan |
| Team unfamiliarity | Medium | Medium | Training, documentation |
| CI/CD failures | Medium | Medium | Thorough testing, gradual rollout |
| Performance degradation | Low | Medium | Load testing, monitoring |

---

## Team & Responsibilities

| Role | Responsibility |
|------|----------------|
| Tech Lead | Overall coordination, architecture decisions |
| Backend Developer | Controller migration, API setup |
| Frontend Developer | UI extraction, SPA development |
| DevOps | CI/CD, infrastructure, deployment |
| QA | Testing, verification, acceptance |

---

## Timeline

| Phase | Duration | Start | End |
|-------|----------|-------|-----|
| Phase 1: NuGet Setup | 1 week | TBD | TBD |
| Phase 2: Repository Setup | 1 week | TBD | TBD |
| Phase 3: Controller Migration | 2 weeks | TBD | TBD |
| Phase 4: UI Extraction | 2 weeks | TBD | TBD |
| Phase 5: CI/CD Setup | 1 week | TBD | TBD |
| Phase 6: Migration Execution | 1 day | TBD | TBD |
| Phase 7: Cleanup | 1 week | TBD | TBD |

**Total Estimated Duration**: 8-10 weeks

---

## Related Documents

- [ADR-0005: Separate API and Admin API](../architecture/adr/ADR-0005-separate-api-and-admin-api.md)
- [ADR-0011: Unified Workspace Orchestration](../architecture/adr/ADR-0011-unified-workspace-orchestration.md)
- [App Components Extraction Analysis](../analysis/app-components-extraction.md)
- [CQRS Migration Guide](../architecture/cqrs-migration-guide.md)

---

## Approval

| Role | Name | Date | Status |
|------|------|------|--------|
| Tech Lead | | | Pending |
| DevOps Lead | | | Pending |
| Product Owner | | | Pending |

---

**Last Updated**: 2025-12-22
