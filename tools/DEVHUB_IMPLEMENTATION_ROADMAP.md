# Mystira DevHub - Implementation Roadmap

## Executive Summary

This document outlines the implementation plan for **Mystira DevHub**, a cross-platform desktop application that replaces the command-line `CosmosConsole` with a modern graphical interface.

**Estimated Timeline**: 4-5 weeks
**Status**: Phase 0 (Planning Complete)
**Next Step**: Phase 1 (Foundation)

---

## What Gets Built

###  All Current CosmosConsole Features
- ✅ Export game sessions to CSV
- ✅ Scenario statistics and analytics
- ✅ Data migrations (Cosmos DB containers + Blob Storage)
- ✅ Infrastructure operations (validate, preview, deploy, destroy)

### ➕ New Features
- ✅ Graphical user interface (React + TailwindCSS + Shadcn/ui)
- ✅ Real-time operation monitoring
- ✅ Bicep template viewer (Monaco Editor)
- ✅ Azure resource status dashboard
- ✅ GitHub Actions workflow monitor
- ✅ What-if analysis viewer
- ✅ Deployment history timeline
- ✅ Secure credential storage (system keychain)

---

## Project Structure (New vs. Existing)

```
tools/
├── Mystira.DevHub/               # NEW: Tauri application (React frontend + Rust backend)
├── Mystira.DevHub.Services/      # NEW: Extracted .NET business logic
├── Mystira.DevHub.CLI/           # NEW: CLI wrapper for Tauri integration
└── Mystira.App.CosmosConsole/    # DEPRECATED: Will add deprecation notice

infrastructure/                    # UNCHANGED: Referenced by DevHub
.github/workflows/                 # UNCHANGED: Triggered by DevHub
```

---

## Implementation Approach

### Option A: Incremental Development (Recommended)
Build and test each component iteratively:
1. **Phase 1**: Foundation (.NET services + CLI wrapper)
2. **Phase 2**: Tauri scaffolding + basic UI
3. **Phase 3**: Cosmos Explorer
4. **Phase 4**: Migration Manager
5. **Phase 5**: Infrastructure Control Panel
6. **Phase 6**: Dashboard & polish

**Pros**:
- Can test and validate each phase
- User can provide feedback early
- Lower risk of rework

**Cons**:
- Takes longer to see complete product
- Requires multiple review cycles

### Option B: Prototype-First
Build a minimal working prototype (Phases 1-2 + basic Infrastructure Panel) to validate UX, then iterate:
1. Foundation + Tauri shell
2. Basic Infrastructure Panel only (validate/preview/deploy)
3. Get feedback, then add remaining features

**Pros**:
- Fastest path to usable tool
- Validates architecture early
- Focus on highest-value feature (Infrastructure)

**Cons**:
- May need refactoring based on feedback

---

## Recommended Next Steps

### Immediate (This Session)
1. **Extract .NET Services** from CosmosConsole into `Mystira.DevHub.Services`
   - Copy interfaces and implementations
   - Update namespaces
   - Test compilation

2. **Create CLI Wrapper** (`Mystira.DevHub.CLI`)
   - JSON command dispatcher
   - Service integration
   - Test with sample commands

3. **Initialize Tauri Project** (`Mystira.DevHub`)
   - Run `npm create tauri-app`
   - Configure React + TypeScript + Vite
   - Set up TailwindCSS

4. **Create Basic Infrastructure Panel UI** (Prototype)
   - Action buttons (Validate/Preview/Deploy)
   - Workflow monitor (GitHub Actions status)
   - Test Tauri ↔ CLI integration

**Estimated Time**: 6-8 hours of focused implementation

### Short Term (Next 1-2 Sessions)
- Complete Phases 3-4 (Cosmos Explorer + Migration Manager)
- Add Monaco Editor for Bicep viewing
- Implement Azure resource status grid
- Add secure credential storage

### Medium Term (Wave 2+)
- Full Dashboard integration
- Deployment history timeline
- Advanced analytics and charts
- Cross-platform installers

---

## Technical Dependencies

### Must Install
- **Node.js 18+** and npm
- **Rust** and Cargo: https://rustup.rs/
- **Tauri CLI**: `cargo install tauri-cli`
- **.NET 9 SDK** (already installed)

### Must Authenticate
- **Azure CLI**: `az login`
- **GitHub CLI**: `gh auth login`

### Development Environment
- **VS Code** recommended with extensions:
  - Rust Analyzer
  - Tauri
  - ES7+ React/Redux/React-Native snippets
  - Tailwind CSS IntelliSense

---

## File Checklist (What Gets Created)

### Phase 1: Foundation
- [ ] `tools/Mystira.DevHub.Services/Mystira.DevHub.Services.csproj`
- [ ] `tools/Mystira.DevHub.Services/Cosmos/ICosmosReportingService.cs`
- [ ] `tools/Mystira.DevHub.Services/Cosmos/CosmosReportingService.cs`
- [ ] `tools/Mystira.DevHub.Services/Migration/IMigrationService.cs`
- [ ] `tools/Mystira.DevHub.Services/Migration/MigrationService.cs`
- [ ] `tools/Mystira.DevHub.Services/Infrastructure/IInfrastructureService.cs`
- [ ] `tools/Mystira.DevHub.Services/Infrastructure/InfrastructureService.cs`
- [ ] `tools/Mystira.DevHub.Services/Data/DevHubDbContext.cs`
- [ ] `tools/Mystira.DevHub.CLI/Mystira.DevHub.CLI.csproj`
- [ ] `tools/Mystira.DevHub.CLI/Program.cs`
- [ ] `tools/Mystira.DevHub.CLI/Commands/*.cs`

### Phase 2: Tauri Application
- [ ] `tools/Mystira.DevHub/package.json`
- [ ] `tools/Mystira.DevHub/src-tauri/Cargo.toml`
- [ ] `tools/Mystira.DevHub/src-tauri/tauri.conf.json`
- [ ] `tools/Mystira.DevHub/src-tauri/src/main.rs`
- [ ] `tools/Mystira.DevHub/src-tauri/src/commands/*.rs`
- [ ] `tools/Mystira.DevHub/src/App.tsx`
- [ ] `tools/Mystira.DevHub/src/components/**/*.tsx`
- [ ] `tools/Mystira.DevHub/tailwind.config.js`
- [ ] `tools/Mystira.DevHub/vite.config.ts`

### Phase 3-6: UI Components
- [ ] `tools/Mystira.DevHub/src/components/cosmos/*.tsx`
- [ ] `tools/Mystira.DevHub/src/components/migration/*.tsx`
- [ ] `tools/Mystira.DevHub/src/components/infrastructure/*.tsx`
- [ ] `tools/Mystira.DevHub/src/components/dashboard/*.tsx`
- [ ] `tools/Mystira.DevHub/src/services/*.ts`
- [ ] `tools/Mystira.DevHub/src/hooks/*.ts`

---

## Success Criteria

### Phase 1 Complete When:
- ✅ All .NET services compile without errors
- ✅ CLI wrapper accepts JSON and returns JSON
- ✅ Can run: `dotnet run --project Mystira.DevHub.CLI -- cosmos export sessions.csv`
- ✅ Unit tests pass for services

### Phase 2 Complete When:
- ✅ Tauri app launches and displays basic UI
- ✅ Can trigger .NET CLI from Tauri Rust backend
- ✅ Frontend receives and displays CLI responses
- ✅ Basic navigation works (sidebar, routing)

### Project Complete When:
- ✅ All CosmosConsole features work in DevHub
- ✅ Infrastructure operations (validate/preview/deploy) work
- ✅ GitHub Actions integration works
- ✅ Monaco Editor displays Bicep files
- ✅ Credentials stored securely in system keychain
- ✅ Builds for Windows, macOS, and Linux
- ✅ User documentation complete

---

## Decision Points

### Now (Before Starting)
**Question**: Which approach should we take?
- **Option A**: Incremental (all phases, 4-5 weeks)
- **Option B**: Prototype-first (Infrastructure Panel only, 1-2 weeks, then iterate)

**Recommendation**: Option B (Prototype-first) to validate the architecture and get a working tool faster.

### After Phase 2
**Question**: Does the Tauri ↔ .NET CLI integration work smoothly?
- **Yes**: Continue with remaining features
- **No**: Consider alternative approaches (gRPC, HTTP API, etc.)

### After Prototype
**Question**: Is the UX intuitive? Is the architecture solid?
- **Yes**: Continue with Cosmos Explorer and Migration Manager
- **Needs Changes**: Refactor based on feedback

---

## Risk Mitigation

### Risk: Tauri ↔ .NET Integration Complexity
**Mitigation**: Build and test Phase 1 thoroughly before moving to UI

### Risk: Monaco Editor Performance
**Mitigation**: Lazy-load Monaco, use virtualization for large files

### Risk: Credential Security
**Mitigation**: Use battle-tested libraries (tauri-plugin-keytar), never log secrets

### Risk: GitHub CLI Rate Limits
**Mitigation**: Implement exponential backoff, display clear error messages

### Risk: Scope Creep
**Mitigation**: Follow phase-based approach, defer nice-to-have features to v2.0

---

## Open Questions

1. **Should we support multiple environments (dev/staging/prod) in one UI?**
   - Recommendation: Yes, with environment selector dropdown

2. **Should we add RBAC (role-based access control)?**
   - Recommendation: Defer to v2.0 (use OS-level security for now)

3. **Should we support custom Bicep template editing in DevHub?**
   - Recommendation: No (read-only), use VS Code for editing

4. **Should we add automated backups before destructive operations?**
   - Recommendation: Yes for v1.0 (critical safety feature)

---

## Next Action

**Proceed with Phase 1 implementation?**
- Creates `Mystira.DevHub.Services` library
- Extracts all services from CosmosConsole
- Builds `Mystira.DevHub.CLI` wrapper
- Tests .NET layer before touching Tauri

**Estimated Time**: 4-6 hours

Type **"proceed"** to continue, or provide feedback/adjustments to the plan.
