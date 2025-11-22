# CI/CD Pipeline Status

## Workflow Path Triggers

All API CI/CD workflows now include paths for shared projects to ensure breaking changes are detected:

### API CI/CD (Dev & Prod)
**Triggered by changes to:**
- `src/Mystira.App.Api/**`
- `src/Mystira.App.Domain/**`
- `src/Mystira.App.Contracts/**` ✅ Added
- `src/Mystira.App.Application/**` ✅ Added
- `src/Mystira.App.Infrastructure.Data/**` ✅ Added
- `.github/workflows/mystira-app-api-cicd-*.yml`

### Admin API CI/CD (Dev & Prod)
**Triggered by changes to:**
- `src/Mystira.App.Admin.Api/**`
- `src/Mystira.App.Domain/**`
- `src/Mystira.App.Contracts/**` ✅ Added
- `src/Mystira.App.Application/**` ✅ Added
- `src/Mystira.App.Infrastructure.Data/**` ✅ Added
- `.github/workflows/mystira-app-admin-api-cicd-*.yml`

### PWA CI/CD
**Includes lint and format checks:**
- `dotnet format --verify-no-changes` - Ensures code formatting
- `dotnet build --no-restore --configuration Release /p:TreatWarningsAsErrors=true` - Code analysis

## Formatting Requirements

All code must pass `dotnet format --verify-no-changes` before CI will pass.

**To fix formatting locally:**
```bash
dotnet format
```

## Build Requirements

All projects must build successfully with:
- No errors
- Warnings treated as errors in Release configuration
- Code style enforcement enabled

