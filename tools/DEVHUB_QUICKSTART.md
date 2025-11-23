# Mystira DevHub - Quick Start Guide

## What is Mystira DevHub?

**Mystira DevHub** is a modern desktop application that replaces the command-line `CosmosConsole` tool. It provides a graphical interface for:

✅ **All CosmosConsole Features**:
- Export game sessions to CSV
- View scenario statistics and analytics
- Migrate data between environments (Cosmos DB + Blob Storage)
- Trigger infrastructure deployments (Bicep/IaC via GitHub Actions)

✨ **New Features**:
- Beautiful graphical interface (React + TailwindCSS)
- Real-time operation monitoring
- Visual Bicep template viewer (Monaco Editor)
- Azure resource status dashboard
- GitHub Actions workflow monitor
- What-if analysis viewer
- Deployment history timeline
- Secure credential storage

---

## Architecture at a Glance

```
┌─────────────────────────────────────┐
│   Mystira DevHub (Tauri App)       │
│                                      │
│  React UI → Rust Backend → .NET CLI│
│             ↓                        │
│          Services                    │
│             ↓                        │
│    Azure / GitHub / Cosmos DB       │
└─────────────────────────────────────┘
```

**Stack**:
- **Frontend**: React 18 + TypeScript + TailwindCSS + Shadcn/ui
- **Desktop Framework**: Tauri (Rust)
- **Business Logic**: .NET 9 (extracted from CosmosConsole)
- **Data Access**: EF Core + Azure SDK

---

## Project Status

📍 **Current Phase**: Planning Complete

**Documentation Created**:
1. ✅ `docs/architecture/DEVHUB_ARCHITECTURE.md` - Full technical architecture (60+ pages)
2. ✅ `tools/DEVHUB_IMPLEMENTATION_ROADMAP.md` - Implementation plan and timeline
3. ✅ `tools/DEVHUB_QUICKSTART.md` - This file

**Implementation Status**:
- [ ] Phase 1: Foundation (.NET services + CLI wrapper)
- [ ] Phase 2: Tauri scaffolding + basic UI
- [ ] Phase 3: Cosmos Explorer
- [ ] Phase 4: Migration Manager
- [ ] Phase 5: Infrastructure Control Panel ⭐
- [ ] Phase 6: Dashboard & polish

---

## Implementation Timeline

### Prototype-First Approach (Recommended)
**Goal**: Working Infrastructure Panel in 1-2 weeks, then iterate

**Week 1**:
- Day 1-2: Extract .NET services, build CLI wrapper
- Day 3-4: Initialize Tauri, basic UI shell
- Day 5: Basic Infrastructure Panel (validate/preview/deploy buttons)

**Week 2**:
- Day 1-2: Monaco Editor integration for Bicep viewing
- Day 3: GitHub Actions workflow monitor
- Day 4: Azure resource status grid
- Day 5: Testing, bug fixes, documentation

**Result**: Usable tool for infrastructure operations

**After Prototype**: Get feedback, then add remaining features (Cosmos Explorer, Migration Manager, Dashboard)

### Full Implementation Approach
**Timeline**: 4-5 weeks
**Result**: All features complete, production-ready

---

## Key Features Preview

### 1. Infrastructure Control Panel ⭐

**Visual Bicep Template Viewer**:
```
📁 infrastructure/dev/
├── 📄 main.bicep (Monaco Editor with syntax highlighting)
└── 📁 modules/
    ├── 📄 cosmos-db.bicep
    ├── 📄 storage.bicep
    └── ...
```

**Action Buttons**:
- 🔍 **Validate Templates** - Check Bicep syntax and ARM validation
- 👁️ **Preview Changes** - What-if analysis (shows resources to be created/modified/deleted)
- 🚀 **Deploy Infrastructure** - Triggers GitHub Actions workflow
- 💥 **Destroy Infrastructure** - Delete all resources (requires confirmation)

**GitHub Actions Monitor**:
```
Workflow: Infrastructure Deployment
Status: ⏳ In Progress (Step 3 of 5)

✅ Check secrets
✅ Validate Bicep templates
⏳ Deploy infrastructure (2m 45s elapsed)
⏸️ Output deployment details
⏸️ Update App Service configuration

[View in GitHub] [View Logs]
```

**Azure Resource Status**:
```
┌──────────────────────┐  ┌──────────────────────┐
│ 🗄️ Cosmos DB         │  │ 💾 Storage Account   │
│ dev-euw-cosmos...    │  │ deveuwstmystira      │
│ ✅ Running            │  │ ✅ Running            │
│ Cost: $2.45/day      │  │ Cost: $0.12/day      │
└──────────────────────┘  └──────────────────────┘
```

### 2. Cosmos Explorer

- Export sessions to CSV with filters
- Visual scenario statistics (charts, graphs)
- Account engagement analytics

### 3. Migration Manager

- Visual source/destination configuration
- Select resources to migrate (scenarios, bundles, media, blobs)
- Real-time progress tracking
- Detailed error reporting

### 4. Dashboard

- Quick action tiles
- Connection status indicators
- Recent operations log

---

## Prerequisites

### Must Have Installed
- ✅ .NET 9 SDK (already have)
- ⬜ Node.js 18+ and npm
- ⬜ Rust and Cargo: https://rustup.rs/
- ⬜ Tauri CLI: `cargo install tauri-cli`

### Must Be Authenticated (for operations)
- ⬜ Azure CLI: `az login`
- ⬜ GitHub CLI: `gh auth login`

### Recommended IDE
- VS Code with extensions:
  - Rust Analyzer
  - Tauri
  - ES7+ React/Redux snippets
  - Tailwind CSS IntelliSense

---

## Next Steps

### Option A: Proceed with Prototype
**Best for**: Quick iteration, early feedback
**Time**: 1-2 weeks
**Deliverable**: Working Infrastructure Panel

```bash
# Start Phase 1: Foundation
# - Extract .NET services
# - Build CLI wrapper
# - Test integration
```

### Option B: Full Implementation
**Best for**: Complete feature set
**Time**: 4-5 weeks
**Deliverable**: All features complete

### Option C: Review & Adjust Plan
**Best for**: Custom requirements
**Action**: Provide feedback on architecture/roadmap docs

---

## Questions?

1. **Will this replace CosmosConsole entirely?**
   - Yes. CosmosConsole will be marked deprecated with a redirect to DevHub.

2. **Can I still use the CLI?**
   - Yes. The underlying `Mystira.DevHub.CLI` can be used standalone.

3. **Does this change the infrastructure folder structure?**
   - No. The `infrastructure/` folder stays exactly where it is. DevHub just provides a GUI for it.

4. **What about the GitHub Actions workflows?**
   - No changes needed. DevHub triggers them via `gh workflow run`.

5. **Is this secure?**
   - Yes. Credentials stored in system keychain (Keychain/Credential Manager/libsecret).
   - Never logs secrets. Uses authenticated Azure/GitHub CLI sessions.

---

## Ready to Build?

Review the architecture and roadmap documents:
- 📖 `docs/architecture/DEVHUB_ARCHITECTURE.md` - Full technical details
- 📋 `tools/DEVHUB_IMPLEMENTATION_ROADMAP.md` - Phase-by-phase plan

Then decide:
- **Type "proceed with prototype"** - Start Phase 1, build Infrastructure Panel first
- **Type "proceed with full implementation"** - Start Phase 1, build all features
- **Type "adjust plan"** - Provide feedback, adjust approach

---

**Let's build something amazing! 🚀**
