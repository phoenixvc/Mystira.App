# Mystira DevHub - Architecture Documentation

## Overview

**Mystira DevHub** is a cross-platform desktop application built with Tauri that provides a unified interface for all development operations, data management, and infrastructure deployment for the Mystira Application Suite.

### Previous State
- **Tool**: `Mystira.App.CosmosConsole` - Command-line only tool
- **Limitations**: No GUI, manual command execution, limited visibility into operations

### New State
- **Tool**: `Mystira.DevHub` - Modern desktop application (Tauri + React)
- **Capabilities**: Visual UI, real-time monitoring, integrated IaC management, comprehensive data operations

---

## Technology Stack

### Frontend
- **Framework**: React 18 with TypeScript
- **Styling**: TailwindCSS + Shadcn/ui component library
- **State Management**: React Query (TanStack Query) for async operations
- **Charts**: Recharts for data visualization
- **Code Editor**: Monaco Editor for Bicep template viewing/editing

### Backend
- **Desktop Framework**: Tauri 1.5+ (Rust)
- **Service Layer**: .NET 9 (extracted from CosmosConsole)
- **Inter-Process Communication**: Tauri Commands → .NET CLI wrapper → Services
- **External Integrations**:
  - Azure SDK (Cosmos DB, Blob Storage)
  - GitHub CLI (workflow triggers)
  - Azure CLI (resource status, what-if analysis)

---

## Project Structure

```
Mystira.App/
├── tools/
│   ├── Mystira.DevHub/                          # NEW: Tauri application
│   │   ├── src/                                 # React frontend
│   │   │   ├── components/
│   │   │   │   ├── layout/
│   │   │   │   │   ├── AppShell.tsx            # Main layout with navigation
│   │   │   │   │   ├── Header.tsx
│   │   │   │   │   └── Sidebar.tsx
│   │   │   │   ├── cosmos/
│   │   │   │   │   ├── ExportPanel.tsx         # CSV export interface
│   │   │   │   │   ├── StatisticsPanel.tsx     # Scenario statistics & charts
│   │   │   │   │   └── ConnectionManager.tsx   # Cosmos connection config
│   │   │   │   ├── migration/
│   │   │   │   │   ├── MigrationDashboard.tsx  # Migration overview
│   │   │   │   │   ├── ResourceSelector.tsx    # Select what to migrate
│   │   │   │   │   ├── MigrationProgress.tsx   # Real-time progress tracking
│   │   │   │   │   └── SourceDestConfig.tsx    # Connection string management
│   │   │   │   ├── infrastructure/
│   │   │   │   │   ├── InfrastructurePanel.tsx # Main IaC control panel
│   │   │   │   │   ├── BicepTemplateViewer.tsx # Monaco editor for Bicep
│   │   │   │   │   ├── ActionButtons.tsx       # Validate/Preview/Deploy/Destroy
│   │   │   │   │   ├── WorkflowMonitor.tsx     # GitHub Actions status
│   │   │   │   │   ├── WhatIfViewer.tsx        # Display what-if results
│   │   │   │   │   ├── ResourceGrid.tsx        # Azure resource status cards
│   │   │   │   │   └── DeploymentHistory.tsx   # Timeline of deployments
│   │   │   │   ├── dashboard/
│   │   │   │   │   ├── Dashboard.tsx           # Home screen
│   │   │   │   │   ├── QuickActions.tsx        # Common operations
│   │   │   │   │   ├── ConnectionStatus.tsx    # Cosmos/Azure/GitHub status
│   │   │   │   │   └── RecentOperations.tsx    # History log
│   │   │   │   └── common/
│   │   │   │       ├── Button.tsx              # Reusable components
│   │   │   │       ├── Card.tsx
│   │   │   │       ├── DataGrid.tsx
│   │   │   │       ├── LoadingSpinner.tsx
│   │   │   │       └── Toast.tsx               # Notifications
│   │   │   ├── services/
│   │   │   │   ├── tauri-api.ts               # Tauri command wrappers
│   │   │   │   ├── types.ts                   # TypeScript type definitions
│   │   │   │   └── api/
│   │   │   │       ├── cosmosService.ts        # Cosmos operations
│   │   │   │       ├── migrationService.ts     # Migration operations
│   │   │   │       └── infraService.ts         # Infrastructure operations
│   │   │   ├── hooks/
│   │   │   │   ├── useCosmosExport.ts         # React Query hooks
│   │   │   │   ├── useMigration.ts
│   │   │   │   └── useInfrastructure.ts
│   │   │   ├── store/
│   │   │   │   └── appStore.ts                # Zustand for global state
│   │   │   ├── App.tsx
│   │   │   ├── main.tsx
│   │   │   └── index.css
│   │   ├── src-tauri/                          # Rust/Tauri backend
│   │   │   ├── src/
│   │   │   │   ├── main.rs                     # Tauri entry point
│   │   │   │   ├── lib.rs                      # Library exports
│   │   │   │   ├── commands/
│   │   │   │   │   ├── mod.rs
│   │   │   │   │   ├── cosmos.rs               # Cosmos DB commands
│   │   │   │   │   ├── migration.rs            # Migration commands
│   │   │   │   │   ├── infrastructure.rs       # Infrastructure commands
│   │   │   │   │   └── system.rs               # System utilities
│   │   │   │   ├── cli_executor/
│   │   │   │   │   ├── mod.rs
│   │   │   │   │   ├── dotnet_runner.rs        # .NET CLI wrapper
│   │   │   │   │   └── process_manager.rs      # Process handling
│   │   │   │   └── state/
│   │   │   │       ├── mod.rs
│   │   │   │       └── app_state.rs            # Shared application state
│   │   │   ├── Cargo.toml
│   │   │   ├── tauri.conf.json
│   │   │   └── build.rs
│   │   ├── package.json
│   │   ├── tsconfig.json
│   │   ├── tailwind.config.js
│   │   ├── vite.config.ts
│   │   └── README.md
│   │
│   ├── Mystira.DevHub.Services/                # NEW: .NET Service Library
│   │   ├── Cosmos/
│   │   │   ├── ICosmosReportingService.cs
│   │   │   ├── CosmosReportingService.cs
│   │   │   └── Models/
│   │   │       ├── GameSessionReport.cs
│   │   │       └── ScenarioStatistics.cs
│   │   ├── Migration/
│   │   │   ├── IMigrationService.cs
│   │   │   ├── MigrationService.cs
│   │   │   └── Models/
│   │   │       └── MigrationResult.cs
│   │   ├── Infrastructure/
│   │   │   ├── IInfrastructureService.cs
│   │   │   ├── InfrastructureService.cs
│   │   │   └── Models/
│   │   │       ├── InfrastructureAction.cs
│   │   │       ├── WorkflowStatus.cs
│   │   │       └── ResourceStatus.cs
│   │   ├── Data/
│   │   │   └── DevHubDbContext.cs              # Renamed from CosmosConsoleDbContext
│   │   ├── Extensions/
│   │   │   └── DataTableExtensions.cs
│   │   └── Mystira.DevHub.Services.csproj
│   │
│   ├── Mystira.DevHub.CLI/                     # NEW: CLI wrapper for Tauri
│   │   ├── Program.cs                          # Accepts JSON commands via stdin
│   │   ├── Commands/
│   │   │   ├── CosmosCommands.cs
│   │   │   ├── MigrationCommands.cs
│   │   │   └── InfrastructureCommands.cs
│   │   ├── Models/
│   │   │   ├── CommandRequest.cs
│   │   │   └── CommandResponse.cs
│   │   ├── appsettings.json
│   │   └── Mystira.DevHub.CLI.csproj
│   │
│   └── Mystira.App.CosmosConsole/              # LEGACY: Will be marked deprecated
│       └── README.md                            # Update to point to DevHub
│
├── infrastructure/                              # STAYS: Already well-organized
│   ├── dev/
│   │   ├── main.bicep
│   │   ├── main.parameters.json
│   │   └── modules/
│   ├── README.md
│   ├── NAMING_AND_OPTIMIZATION.md
│   └── SECRETS_SETUP_GUIDE.md
│
└── .github/workflows/
    └── infrastructure-deploy-dev.yml            # STAYS: Referenced by DevHub
```

---

## Architecture Diagrams

### Component Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Mystira DevHub (Tauri)                   │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │          React Frontend (TypeScript)                   │ │
│  │                                                         │ │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐            │ │
│  │  │Dashboard │  │ Cosmos   │  │Migration │            │ │
│  │  │          │  │ Explorer │  │ Manager  │            │ │
│  │  └──────────┘  └──────────┘  └──────────┘            │ │
│  │                                                         │ │
│  │  ┌──────────────────────────────────────────────────┐ │ │
│  │  │   Infrastructure Control Panel                   │ │ │
│  │  │   - Bicep Viewer (Monaco Editor)                │ │ │
│  │  │   - Action Buttons (Validate/Preview/Deploy)    │ │ │
│  │  │   - GitHub Actions Monitor                       │ │ │
│  │  │   - Azure Resource Grid                          │ │ │
│  │  └──────────────────────────────────────────────────┘ │ │
│  │                                                         │ │
│  │         Tauri Commands (invoke)                        │ │
│  └─────────────────────────────────────────────────────────┘ │
│                           ↓                                   │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │           Rust Backend (Tauri Core)                     │ │
│  │                                                          │ │
│  │  ┌──────────────────────────────────────────────────┐  │ │
│  │  │  Command Handlers                                 │  │ │
│  │  │  - cosmos::export_sessions()                     │  │ │
│  │  │  - migration::migrate_resources()                │  │ │
│  │  │  - infrastructure::trigger_workflow()            │  │ │
│  │  └──────────────────────────────────────────────────┘  │ │
│  │                           ↓                              │ │
│  │  ┌──────────────────────────────────────────────────┐  │ │
│  │  │  CLI Executor                                     │  │ │
│  │  │  - Spawn .NET processes                           │  │ │
│  │  │  - Stream stdout/stderr                           │  │ │
│  │  │  - Handle process lifecycle                       │  │ │
│  │  └──────────────────────────────────────────────────┘  │ │
│  └──────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
                           ↓
┌──────────────────────────────────────────────────────────────┐
│          Mystira.DevHub.CLI (.NET 9)                         │
│                                                               │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Accepts JSON via stdin:                               │  │
│  │  {                                                      │  │
│  │    "command": "cosmos.export",                         │  │
│  │    "args": { "output": "sessions.csv" }                │  │
│  │  }                                                      │  │
│  └────────────────────────────────────────────────────────┘  │
│                           ↓                                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  Command Dispatcher (routes to appropriate service)    │  │
│  └────────────────────────────────────────────────────────┘  │
│                           ↓                                   │
│  ┌────────────────────────────────────────────────────────┐  │
│  │        Mystira.DevHub.Services                          │  │
│  │                                                          │  │
│  │  - CosmosReportingService                              │  │
│  │  - MigrationService                                     │  │
│  │  - InfrastructureService                               │  │
│  └────────────────────────────────────────────────────────┘  │
│                           ↓                                   │
│  Returns JSON:                                               │
│  {                                                            │
│    "success": true,                                          │
│    "result": { ... },                                        │
│    "error": null                                             │
│  }                                                            │
└──────────────────────────────────────────────────────────────┘
                           ↓
┌──────────────────────────────────────────────────────────────┐
│              External Services                                │
│                                                               │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │ Cosmos DB│  │  Azure   │  │  GitHub  │  │  Azure   │    │
│  │          │  │  Blob    │  │  Actions │  │   CLI    │    │
│  │          │  │ Storage  │  │          │  │          │    │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘    │
└──────────────────────────────────────────────────────────────┘
```

### Data Flow Example: Infrastructure Deployment

```
1. User clicks "Deploy Infrastructure" button
                ↓
2. React → Tauri Command: invoke('infra_deploy', { action: 'deploy' })
                ↓
3. Rust handler: infrastructure::trigger_deployment()
                ↓
4. CLI Executor spawns: Mystira.DevHub.CLI infrastructure deploy
                ↓
5. CLI → InfrastructureService.DeployAsync()
                ↓
6. InfrastructureService → GitHub CLI: gh workflow run infrastructure-deploy-dev.yml
                ↓
7. GitHub Actions workflow starts
                ↓
8. CLI returns workflow ID and status URL to Rust
                ↓
9. Rust streams updates back to React
                ↓
10. UI updates with:
    - Workflow link
    - Real-time progress
    - Resource status grid
```

---

## Feature Specifications

### 1. Dashboard (Home Screen)

**Purpose**: Central hub for quick access and status overview

**Components**:
- **Quick Actions Grid**:
  - Export Sessions to CSV
  - Run Scenario Stats
  - Start Migration
  - Validate Bicep Templates
  - Deploy Infrastructure

- **Connection Status Indicators**:
  - Cosmos DB: Connected / Disconnected (with connection string management)
  - Azure CLI: Authenticated / Not Authenticated
  - GitHub CLI: Authenticated / Not Authenticated

- **Recent Operations Log**:
  - Last 10 operations with timestamps
  - Status (Success / Failed / In Progress)
  - Quick link to detailed results

### 2. Cosmos Explorer

**Export Panel**:
- Container selection dropdown
- Date range filter (optional)
- Output format: CSV, JSON
- Export button with progress indicator
- Download or save to specific path

**Statistics Panel**:
- Scenario completion rates (bar chart)
- Sessions over time (line chart)
- Top scenarios by engagement (pie chart)
- Account breakdown table with sorting

### 3. Migration Manager

**Source/Destination Configuration**:
- Visual connection string editor
- Test connection buttons
- Save/load configurations
- Environment presets (dev → staging, staging → prod)

**Resource Selection**:
- Checkboxes for:
  - ☑ Scenarios
  - ☑ Content Bundles
  - ☑ Media Assets Metadata
  - ☑ Blob Storage Files
- "Select All" / "Select None" buttons

**Migration Progress**:
- Overall progress bar
- Per-resource progress
- Real-time log stream
- Success/failure counts
- Error messages with details
- Cancel button (graceful cancellation)

**Result Summary**:
- Total items processed
- Success/failure counts
- Duration
- Detailed error list
- Export results to JSON

### 4. Infrastructure Control Panel ⭐

**Bicep Template Viewer**:
- Tree view of all Bicep files:
  ```
  📁 infrastructure/dev/
  ├── 📄 main.bicep
  └── 📁 modules/
      ├── 📄 cosmos-db.bicep
      ├── 📄 storage.bicep
      ├── 📄 app-service.bicep
      ├── 📄 communication-services.bicep
      ├── 📄 log-analytics.bicep
      └── 📄 application-insights.bicep
  ```
- Monaco Editor with:
  - Bicep syntax highlighting
  - Line numbers
  - Read-only mode (security)
  - Find/search functionality

**Action Buttons**:
```tsx
┌─────────────────────────────────────────────────────────────┐
│  🔍 Validate Templates                                      │
│  Runs: az deployment group validate                         │
│  Shows: Validation errors/warnings                          │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  👁️ Preview Changes (What-If)                               │
│  Runs: az deployment group what-if                          │
│  Shows: Resources to be created/modified/deleted            │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  🚀 Deploy Infrastructure                                    │
│  Triggers: GitHub Actions workflow (infrastructure-deploy)  │
│  Requires: Confirmation dialog                              │
│  Shows: Workflow monitor                                    │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  💥 Destroy Infrastructure (DANGER)                         │
│  Triggers: GitHub Actions workflow with destroy=true        │
│  Requires: Type "DELETE" to confirm + additional dialog     │
│  Shows: Deletion progress                                   │
└─────────────────────────────────────────────────────────────┘
```

**What-If Viewer**:
- Diff-style visualization:
  ```
  Resources to be created: (5)
  + Cosmos DB Account: dev-euw-cosmos-mystira
  + Storage Account: deveuwstmystira
  + App Service: dev-euw-app-mystira-api
  + Log Analytics: dev-euw-log-mystira
  + Application Insights: dev-euw-ai-mystira

  Resources to be modified: (0)

  Resources to be deleted: (0)
  ```
- Color coding:
  - Green: Created
  - Yellow: Modified
  - Red: Deleted
- Resource details on click

**GitHub Actions Workflow Monitor**:
- Workflow run status: Queued / In Progress / Success / Failed
- Step-by-step progress:
  ```
  ✅ Check secrets availability
  ✅ Validate Bicep templates
  ⏳ Deploy Infrastructure (in progress...)
     └─ Creating Cosmos DB...
  ⏸️ Output deployment details (pending)
  ```
- Live log streaming (optional)
- Link to GitHub Actions page
- Estimated time remaining

**Azure Resource Status Grid**:
- Cards for each resource:
  ```
  ┌──────────────────────────────────┐
  │ 🗄️ Cosmos DB                     │
  │ dev-euw-cosmos-mystira          │
  │ Status: ✅ Running               │
  │ Region: West Europe             │
  │ Cost (today): $2.45             │
  │ [View in Portal]                │
  └──────────────────────────────────┘
  ```
- Status indicators:
  - ✅ Green: Healthy
  - ⚠️ Yellow: Warning
  - ❌ Red: Failed
  - ⏸️ Gray: Stopped/Deallocated
- Quick actions per resource:
  - View in Azure Portal
  - View metrics
  - View logs

**Deployment History Timeline**:
- Chronological list of deployments:
  ```
  📅 2025-11-23 14:30 UTC
  🚀 Deploy Infrastructure
  ✅ Success (Duration: 4m 32s)
  [View Details] [View in GitHub]

  📅 2025-11-22 10:15 UTC
  🔍 Validate Templates
  ✅ Success (Duration: 23s)
  [View Details]

  📅 2025-11-20 16:45 UTC
  👁️ Preview Changes
  ✅ Success (Duration: 1m 12s)
  [View What-If Results]
  ```

### 5. Configuration Management

**Secrets & Connection Strings**:
- Stored securely in system keychain (using tauri-plugin-keytar)
- Visual editor with "Show/Hide" toggle
- Test connection buttons
- Import from environment variables
- Export to .env file (with warnings)

**Application Settings**:
- Default output paths
- Notification preferences
- Theme (Light/Dark/Auto)
- Log level
- Auto-update preferences

---

## Security Considerations

### Credential Storage
- **Never** store secrets in application config files
- Use system keychain (Keychain on macOS, Credential Manager on Windows, libsecret on Linux)
- Encrypt sensitive data in memory
- Clear secrets from memory after use

### Bicep Template Viewing
- Read-only Monaco Editor (prevent accidental modifications)
- No direct editing from DevHub (use VS Code or preferred IDE)
- Display file hash to verify integrity

### Infrastructure Operations
- Require explicit confirmation for destructive actions (destroy)
- Log all infrastructure operations
- Display clear warnings before deploying to production

### GitHub/Azure CLI Integration
- Verify CLI tools are authenticated before operations
- Never capture or log authentication tokens
- Use existing user's authenticated sessions

---

## Implementation Phases

### Phase 1: Foundation (Week 1)
- [ ] Create Mystira.DevHub.Services library
- [ ] Extract and refactor services from CosmosConsole
- [ ] Create DevHubDbContext
- [ ] Build Mystira.DevHub.CLI wrapper
- [ ] Test CLI wrapper with JSON input/output

### Phase 2: Tauri Application Scaffolding (Week 1-2)
- [ ] Initialize Tauri project
- [ ] Set up React + TypeScript + Vite
- [ ] Configure TailwindCSS and Shadcn/ui
- [ ] Create basic layout (AppShell, Header, Sidebar)
- [ ] Implement Rust command handlers (skeleton)
- [ ] Test Tauri ↔ .NET CLI communication

### Phase 3: Cosmos Explorer (Week 2)
- [ ] Build Export Panel UI
- [ ] Implement export-to-CSV functionality
- [ ] Build Statistics Panel UI
- [ ] Integrate Recharts for visualizations
- [ ] Add connection management UI
- [ ] Test end-to-end Cosmos operations

### Phase 4: Migration Manager (Week 2-3)
- [ ] Build Migration Dashboard UI
- [ ] Implement Source/Dest configuration UI
- [ ] Create Resource Selector with checkboxes
- [ ] Build Progress Tracker with real-time updates
- [ ] Add error handling and retry logic
- [ ] Test full migration workflow

### Phase 5: Infrastructure Control Panel (Week 3-4) ⭐
- [ ] Integrate Monaco Editor for Bicep viewing
- [ ] Build file tree navigation
- [ ] Implement Action Buttons (Validate/Preview/Deploy/Destroy)
- [ ] Create What-If Viewer with diff visualization
- [ ] Build GitHub Actions Workflow Monitor
- [ ] Implement Azure Resource Status Grid
- [ ] Create Deployment History Timeline
- [ ] Add confirmation dialogs for destructive actions
- [ ] Test all infrastructure operations

### Phase 6: Dashboard & Integration (Week 4)
- [ ] Build Dashboard with Quick Actions
- [ ] Implement Connection Status Indicators
- [ ] Create Recent Operations Log
- [ ] Add cross-component navigation
- [ ] Implement notification system (toasts)
- [ ] Add dark mode support

### Phase 7: Configuration & Security (Week 4-5)
- [ ] Integrate tauri-plugin-keytar for credential storage
- [ ] Build Configuration Management UI
- [ ] Implement secret encryption
- [ ] Add application settings panel
- [ ] Security audit

### Phase 8: Testing & Polish (Week 5)
- [ ] End-to-end testing of all features
- [ ] UI/UX refinements
- [ ] Performance optimization
- [ ] Error handling improvements
- [ ] Documentation (user guide, developer guide)
- [ ] Create installer packages (Windows, macOS, Linux)

---

## Migration Path from CosmosConsole

### Step 1: Deprecate Old Console
- Update `Mystira.App.CosmosConsole/README.md`:
  ```markdown
  # ⚠️ DEPRECATED

  This console application has been replaced by **Mystira DevHub**,
  a modern desktop application with a graphical interface.

  Please use Mystira DevHub for all development operations.

  See: tools/Mystira.DevHub/README.md
  ```

### Step 2: Extract Services
- All business logic moved to `Mystira.DevHub.Services`
- Console-specific code (CLI parsing) discarded
- Services remain unit-testable

### Step 3: Create CLI Wrapper
- `Mystira.DevHub.CLI` accepts JSON commands
- Maps to service calls
- Returns JSON responses
- Tauri calls this CLI wrapper

### Step 4: Build Tauri UI
- Progressive enhancement: start with basic features
- Add advanced features iteratively
- Maintain backward compatibility with .NET services

---

## Future Enhancements

### v2.0 Features
- **AI Assistant**: Natural language commands ("Export sessions from last week")
- **Scenario Editor**: Visual scenario creation tool
- **Real-Time Collaboration**: Multiple users working on data migrations
- **Advanced Analytics**: Predictive insights, anomaly detection
- **Mobile Companion App**: Monitor deployments from phone
- **Plugin System**: Extend functionality with custom plugins

### v2.1 Features
- **Cost Optimization Advisor**: Recommendations to reduce Azure costs
- **Resource Tagging Manager**: Visual tag management across resources
- **Backup & Restore**: Automated Cosmos DB backups with restore UI
- **Performance Profiler**: Analyze and optimize Cosmos DB queries

---

## Getting Started (for Developers)

### Prerequisites
- .NET 9 SDK
- Node.js 18+ and npm
- Rust and Cargo
- Tauri CLI: `cargo install tauri-cli`
- Azure CLI (for infrastructure operations)
- GitHub CLI (for workflow triggers)

### Build Instructions

```bash
# 1. Build .NET services
cd tools/Mystira.DevHub.Services
dotnet build

# 2. Build .NET CLI wrapper
cd ../Mystira.DevHub.CLI
dotnet build

# 3. Install frontend dependencies
cd ../Mystira.DevHub
npm install

# 4. Run in development mode
npm run tauri dev

# 5. Build production bundle
npm run tauri build
```

### Project Structure Summary
```
Mystira.DevHub/
├── src/              # React frontend
├── src-tauri/        # Rust backend
├── dist/             # Built frontend (generated)
└── src-tauri/target/ # Rust build output (generated)
```

---

## Questions & Support

For questions about Mystira DevHub architecture or implementation:
1. Review this document and related PRDs
2. Check `tools/Mystira.DevHub/README.md` for user documentation
3. Contact the development team

---

**Document Version**: 1.0
**Last Updated**: 2025-11-23
**Status**: In Progress (Phase 1)
