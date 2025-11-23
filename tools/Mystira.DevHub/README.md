# Mystira DevHub

**Version 0.1.0** | A modern cross-platform desktop application for Mystira development operations

Built with **Tauri**, **React**, **TypeScript**, and **.NET 9**

---

## 🚀 Quick Start

### Option 1: Launch from Repository Root (Recommended)

```bash
# From repository root
.\start.ps1    # Windows PowerShell
# OR
./start.ps1    # Cross-platform (if PowerShell Core installed)
```

This will:

- Build the DevHub frontend
- Launch the Tauri application
- Auto-detect repository root from current path
- Allow you to manage all services from within DevHub

### Option 2: Launch from DevHub Directory

```bash
# Navigate to DevHub directory
cd tools/Mystira.DevHub

# Install dependencies (first time only)
npm install

# Build frontend
npm run build

# Run in development mode
npm run tauri:dev
```

**Prerequisites**: .NET 9 SDK, Node.js 18+, Rust, GitHub CLI (`gh`), Azure CLI (`az`)

📘 **See [QUICKSTART.md](QUICKSTART.md) for detailed setup and troubleshooting.**

### Using the Service Manager

Once DevHub is running:

1. **Navigate to Services Tab**: Click "Services" in the sidebar

2. **Configure Repository Root**:

   - Repository root auto-detects from current path
   - Click "Browse..." to manually select a different location
   - Optionally enable "Use current branch directory" to run from branch-specific paths

3. **Start Services**:

   - Click "Start All" to start all services at once
   - Or start individual services with their "Start" buttons

4. **View Services**:

   - Click "Open in Webview" to view in embedded Chromium window
   - Click "Open in External Browser" for system browser
   - Click "Show Logs" to see real-time console output

5. **Monitor**: Service status updates automatically every 2 seconds

---

## ✨ Features

### 🚀 Service Manager (NEW!)

- **Unified Service Control**: Start, stop, and monitor all development services from one place
- **Embedded Chromium Webviews**: View APIs and frontends directly in DevHub windows (no external browsers needed)
- **Real-time Console Streaming**: See live stdout/stderr output from all services
- **Repository Root Management**:
  - Auto-detects repository root from current path
  - Browse button to manually select repository location
  - Option to use current git branch directory vs main repo root
- **Bulk Operations**: Start All / Stop All buttons for quick service management
- **Service Status**: Real-time monitoring of API, Admin API, and PWA services
- **Build Before Run**: Automatically builds services before starting to catch errors early

### 🏠 Dashboard

- **Quick Actions**: One-click access to common operations
- **Connection Status**: Real-time monitoring of Cosmos DB, Azure CLI, GitHub CLI, and Blob Storage
- **Recent Operations**: Activity log with timestamps and status indicators
- **System Info**: Tips, documentation links, and performance notes

### 📊 Cosmos Explorer

- **Export Sessions**: Export game sessions to CSV with native file dialog
- **Statistics Panel**:
  
  - Visual analytics with color-coded completion rates
  - Scenario-by-scenario breakdown
  - Per-account statistics
  - Auto-loading data on component mount
  - Expandable detail cards

### 🔄 Migration Manager

- **Multi-Step Wizard**:

  - Step 1: Configure source/destination connection strings
  - Step 2: Select resources (Scenarios, Content Bundles, Media Metadata, Blob Storage)
  - Step 3: Real-time migration progress
  - Step 4: Detailed success/failure results
- **Resource Selection**: Granular control over what to migrate
- **Error Handling**: Comprehensive error reporting and recovery guidance
- **Validation**: Pre-migration validation of connection strings

### ⚙️ Infrastructure Control Panel

- **Tabbed Interface**:

  - **Actions**: Validate, Preview, Deploy, and Destroy infrastructure
  - **Bicep Templates**: Monaco Editor with file tree navigation
  - **Azure Resources**: Resource grid with health status and cost tracking
  - **Deployment History**: Timeline of infrastructure operations

- **Actions Tab**:
  - 🔍 Validate Bicep templates
  - 👁️ Preview changes (What-If analysis)
  - 🚀 Deploy infrastructure via GitHub Actions
  - 💥 Destroy infrastructure (multi-layer confirmation)
  - Workflow status monitoring

- **Bicep Viewer**:
  - Read-only Monaco Editor with syntax highlighting
  - File tree navigation (infrastructure/dev)
  - Collapsible folder structure
  - Refresh capability

- **What-If Viewer**:
  - Color-coded change visualization (create/modify/delete)
  - Summary cards with change counts
  - Expandable resource details
  - Warning banners for destructive changes

- **Resource Grid**:
  - Azure resource cards with health indicators
  - Cost tracking (daily cost per resource)
  - Region and property information
  - "View in Portal" links
  - Summary statistics

- **Deployment History**:
  - Chronological timeline
  - Filterable by action type
  - Status badges and duration tracking
  - Links to GitHub Actions and Azure Portal

---

## 🏗️ Architecture

``` text
┌─────────────────────────────────────────┐
│     React Frontend (TypeScript)         │
│  - Dashboard                             │
│  - Cosmos Explorer                       │
│  - Migration Manager                     │
│  - Infrastructure Panel                  │
└─────────────────┬───────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│      Tauri Backend (Rust)                │
│  - Tauri Commands                        │
│  - Process Management                    │
│  - File System Access                    │
└─────────────────┬───────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│    Mystira.DevHub.CLI (.NET 9)          │
│  - JSON stdin/stdout wrapper            │
│  - Command routing                       │
└─────────────────┬───────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│   Mystira.DevHub.Services (.NET 9)      │
│  - CosmosReportingService                │
│  - MigrationService                      │
│  - InfrastructureService                 │
└─────────────────┬───────────────────────┘
                  ↓
┌─────────────────────────────────────────┐
│       External Services                  │
│  - Azure Cosmos DB                       │
│  - Azure Blob Storage                    │
│  - GitHub Actions (via gh CLI)           │
│  - Azure Resource Manager (via az CLI)   │
└──────────────────────────────────────────┘
```

---

## 📋 Prerequisites

### Required Software

| Tool       | Version | Purpose                   |
| ---------- | ------- | ------------------------- |
| .NET SDK   | 9.0+    | Backend services          |
| Node.js    | 18+     | Frontend build            |
| npm        | 8+      | Package manager           |
| Rust       | Latest  | Tauri backend             |
| Cargo      | Latest  | Rust build tool           |
| GitHub CLI | Latest  | Infrastructure operations |
| Azure CLI  | Latest  | Resource management       |

### Installation

**macOS**:

```bash
brew install dotnet-sdk node rust gh azure-cli
```

**Windows**:

- .NET: Download from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
- Node.js: Download from [nodejs.org](https://nodejs.org/)
- Rust: Download from [rustup.rs](https://rustup.rs/)
- GitHub CLI: `winget install GitHub.cli`
- Azure CLI: `winget install Microsoft.AzureCLI`

**Linux** (Ubuntu/Debian):

```bash
# .NET
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0

# Node.js
curl -fsSL https://deb.nodesource.com/setup_18.x | sudo -E bash -
sudo apt-get install -y nodejs

# Rust
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh

# GitHub CLI
sudo apt install gh

# Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
```

### Authentication

Before using DevHub, authenticate your CLI tools:

```bash
# GitHub CLI
gh auth login

# Azure CLI
az login
az account set --subscription "Your Subscription Name"

# Verify
gh auth status
az account show
```

---

## 🔧 Installation & Setup

### 1. Clone Repository

```bash
git clone https://github.com/phoenixvc/Mystira.App.git
cd Mystira.App/tools/Mystira.DevHub
```

### 2. Install Dependencies

```bash
# Install npm packages
npm install

# Build .NET services
cd ../Mystira.DevHub.Services
dotnet build

# Build .NET CLI
cd ../Mystira.DevHub.CLI
dotnet build

# Return to DevHub directory
cd ../Mystira.DevHub
```

### 3. Run Development Server

```bash
npm run tauri:dev
```

This will:

- Start Vite dev server (port 5173)
- Compile Rust backend
- Launch Tauri application
- Enable hot-reload for React changes

### 4. Build for Production

```bash
npm run tauri:build
```

**Output locations**:

- **Windows**: `src-tauri/target/release/bundle/msi/`
- **macOS**: `src-tauri/target/release/bundle/dmg/`
- **Linux**: `src-tauri/target/release/bundle/deb/` or `appimage/`

---

## 📂 Project Structure

``` text
Mystira.DevHub/
├── src/                              # React frontend
│   ├── components/
│   │   ├── Dashboard.tsx             # Home screen with quick actions
│   │   ├── ServiceManager.tsx        # Service control with embedded webviews
│   │   ├── WebViewPanel.tsx          # Embedded Chromium webview component
│   │   ├── CosmosExplorer.tsx        # Cosmos DB operations
│   │   ├── ExportPanel.tsx           # CSV export interface
│   │   ├── StatisticsPanel.tsx       # Analytics visualizations
│   │   ├── MigrationManager.tsx      # Multi-step migration wizard
│   │   ├── InfrastructurePanel.tsx   # Infrastructure control
│   │   ├── BicepViewer.tsx           # Monaco Editor for Bicep
│   │   ├── WhatIfViewer.tsx          # Diff visualization
│   │   ├── ResourceGrid.tsx          # Azure resource cards
│   │   └── DeploymentHistory.tsx     # Timeline of deployments
│   ├── App.tsx                       # Main app with navigation
│   ├── main.tsx                      # React entry point
│   └── index.css                     # Global styles
│
├── src-tauri/                        # Rust/Tauri backend
│   ├── src/
│   │   └── main.rs                   # Tauri commands + service management
│   ├── Cargo.toml                    # Rust dependencies (includes tokio for async)
│   ├── tauri.conf.json               # Tauri configuration
│   └── build.rs                      # Build script
│
├── package.json                      # Node dependencies
├── vite.config.ts                    # Vite configuration
├── tailwind.config.js                # TailwindCSS config
├── tsconfig.json                     # TypeScript config
├── README.md                         # This file
├── SECURITY.md                       # Security guidelines
└── CONFIGURATION.md                  # Configuration guide

../Mystira.DevHub.Services/           # .NET 9 Services
├── Cosmos/
│   ├── CosmosReportingService.cs
│   └── Models/
├── Migration/
│   ├── MigrationService.cs
│   └── Models/
└── Infrastructure/
    ├── InfrastructureService.cs
    └── Models/

../Mystira.DevHub.CLI/                # .NET 9 CLI Wrapper
├── Program.cs                        # JSON stdin/stdout handler
├── Commands/
│   ├── CosmosCommands.cs
│   ├── MigrationCommands.cs
│   └── InfrastructureCommands.cs
└── Models/
```

---

## 🎯 Usage Guide

### Service Manager

**Navigate**: Dashboard → Services (sidebar)

**Starting Services**:

1. Repository root auto-detects from current path (or click "Browse..." to change)
2. Optionally check "Use current branch directory" to run from branch-specific paths
3. Click "Start All" to start all services, or start individual services
4. Services automatically build before running to catch errors early

**Viewing Services**:

- **Embedded Webview**: Click "Open in Webview" to open in a Chromium window within DevHub
- **External Browser**: Click "Open in External Browser" for system default browser
- **Console Logs**: Click "Show Logs" to see real-time stdout/stderr output
  - Logs auto-scroll to latest
  - Color-coded (green for stdout, red for stderr)
  - "Clear Logs" button to reset console view

**Stopping Services**:

- Click "Stop All" to stop all running services
- Or stop individual services with their "Stop" buttons

**Service Status**:

- Real-time status updates every 2 seconds
- Shows running/stopped state, port numbers, and URLs
- Process monitoring ensures accurate status

### Dashboard

**Launch DevHub** → Opens on Dashboard by default

**Quick Actions**:

- Click any gradient card to navigate to that feature
- Monitor connection status at the top
- View recent operations at the bottom

### Cosmos Explorer

**Navigate**: Dashboard → Cosmos Explorer (sidebar)

**Export Sessions**:

1. Click "Export Sessions" tab
2. Select output path via file dialog
3. Click "Export" button
4. View row count and file path in result

**View Statistics**:

1. Click "Statistics" tab
2. Statistics auto-load on mount
3. Click scenario cards to expand account details
4. View color-coded completion rates

### Migration Manager

**Navigate**: Dashboard → Migration Manager (sidebar)

**Run Migration**:

1. **Configure**: Enter source/dest connection strings
2. **Select Resources**: Choose what to migrate (checkboxes)
3. **Migrate**: Watch real-time progress
4. **Results**: View detailed success/failure report

**Migration Types**:

- ✅ Scenarios (Cosmos DB)
- ✅ Content Bundles (Cosmos DB)
- ✅ Media Assets Metadata (Cosmos DB)
- ✅ Blob Storage Files (Azure Storage)

### Infrastructure Panel

**Navigate**: Dashboard → Infrastructure (sidebar)

**Actions Tab**:

- **Validate**: Check Bicep syntax and ARM validation
- **Preview**: Run what-if analysis (shows changes)
- **Deploy**: Trigger GitHub Actions workflow
- **Destroy**: Delete all resources (requires "DELETE" confirmation)

**Bicep Templates Tab**:

- Browse file tree (infrastructure/dev)
- Click files to view in Monaco Editor
- Read-only mode for safety

**Azure Resources Tab**:

- View resource cards with health status
- See daily costs per resource
- Click "View in Portal" to open Azure Portal

**Deployment History Tab**:

- Filter by action type
- View timestamps and durations
- Click "Details" for more info
- Follow links to GitHub Actions

---

## ⚙️ Configuration

See **[CONFIGURATION.md](./CONFIGURATION.md)** for detailed configuration options.

### Quick Configuration

**Environment Variables** (recommended for development):

```bash
export SOURCE_COSMOS_CONNECTION="AccountEndpoint=https://..."
export DEST_COSMOS_CONNECTION="AccountEndpoint=https://..."
export SOURCE_STORAGE_CONNECTION="DefaultEndpointsProtocol=https;..."
export DEST_STORAGE_CONNECTION="DefaultEndpointsProtocol=https;..."
```

**Alternative**: Enter connection strings in the UI when needed.

---

## 🔒 Security

See **[SECURITY.md](./SECURITY.md)** for comprehensive security guidelines.

### Key Security Features

- ✅ No credential persistence (session-only)
- ✅ Read-only Bicep viewer
- ✅ Multi-layer confirmation for destructive operations
- ✅ CLI authentication reliance (no token storage)
- ✅ HTTPS/TLS encryption for all Azure connections
- ✅ Audit logging for all operations

### Best Practices

- Never commit credentials to git
- Use environment variables for secrets
- Rotate Azure keys regularly
- Test migrations in non-production first
- Backup before destructive operations

---

## 🐛 Troubleshooting

### Common Issues

#### **"dotnet: command not found"**

```bash
# Install .NET 9 SDK
# macOS: brew install dotnet-sdk
# Windows: Download from [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
# Linux: See installation section above
```

#### **"gh: command not found"**

```bash
# Install GitHub CLI
gh --version  # Should return version number
```

#### **"Failed to spawn dotnet process"**

- Verify .NET installed: `dotnet --version`
- Check CLI project exists: `ls ../Mystira.DevHub.CLI`
- Rebuild CLI: `cd ../Mystira.DevHub.CLI && dotnet build`

#### **GitHub Actions not triggering**

```bash
# Verify authentication
gh auth status

# Check repository access
gh repo view phoenixvc/Mystira.App
```

#### **Azure CLI errors**

```bash
# Re-authenticate
az login

# Set subscription
az account set --subscription "Your Subscription"
```

#### **Monaco Editor not loading**

```bash
# Reinstall dependencies
rm -rf node_modules package-lock.json
npm install
```

### Debug Mode

**React DevTools**:

- Right-click in app → Inspect Element
- Console tab shows React logs

**Rust Logs**:

- Terminal output where `tauri:dev` runs
- Use `println!()` for debugging

**.NET CLI Logs**:

- Errors shown in app UI response boxes
- Check stdout/stderr from spawned process

### Test .NET CLI Directly

```bash
cd ../Mystira.DevHub.CLI

# Test command
echo '{"command":"cosmos.stats","args":{}}' | dotnet run
```

---

## 🧪 Development

### Hot Reload

- **React/TypeScript**: Automatic via Vite
- **Rust backend**: Automatic recompilation
- **.NET CLI**: Manual rebuild required

### Adding New Features

**1. Add Tauri Command** (`src-tauri/src/main.rs`):

```rust
#[tauri::command]
async fn my_feature(param: String) -> Result<CommandResponse, String> {
    execute_devhub_cli("my.feature".to_string(), json!({ "param": param })).await
}
```

**2. Register Command**:

```rust
.invoke_handler(tauri::generate_handler![
    my_feature,
    // ... existing
])
```

**3. Add .NET Handler** (`Mystira.DevHub.CLI/Commands/`):

```csharp
public async Task<CommandResponse> MyFeatureAsync(JsonElement args) {
    // Implementation
}
```

**4. Route in CLI** (`Program.cs`):

```csharp
"my.feature" => await myCommands.MyFeatureAsync(request.Args)
```

**5. Call from React**:

```typescript
const response = await invoke('my_feature', { param: 'value' });
```

### Code Style

- **React**: Functional components with hooks
- **TypeScript**: Strict type checking
- **Rust**: Follow Clippy recommendations
- **C#**: Follow .NET conventions

---

## 📚 Documentation

- **[DEVHUB_ARCHITECTURE.md](../../docs/architecture/DEVHUB_ARCHITECTURE.md)**: Complete technical architecture
- **[SECURITY.md](./SECURITY.md)**: Security best practices and guidelines
- **[CONFIGURATION.md](./CONFIGURATION.md)**: Detailed configuration options
- **[DEVHUB_IMPLEMENTATION_ROADMAP.md](../DEVHUB_IMPLEMENTATION_ROADMAP.md)**: Implementation phases

---

## 🗺️ Roadmap

### ✅ Completed (v0.1.0)

- [x] **Phase 1**: .NET Services extraction and CLI wrapper
- [x] **Phase 2**: Tauri application with Infrastructure Panel
- [x] **Phase 3**: Cosmos Explorer (Export + Statistics)
- [x] **Phase 4**: Migration Manager with multi-step wizard
- [x] **Phase 5**: Infrastructure Panel enhancements (Monaco, Resource Grid, History)
- [x] **Phase 6**: Dashboard with Quick Actions
- [x] **Phase 7**: Security and Configuration documentation
- [x] **Phase 8**: Service Manager with embedded webviews and console streaming
- [x] **Phase 9**: Repository root auto-detection and branch support
- [x] **Phase 10**: Unified launcher (start.ps1) for single-entry development

### 🔜 Future Enhancements

- [ ] **System Keychain Integration**: Secure credential storage
- [ ] **Toast Notifications**: Real-time operation feedback
- [ ] **Dark Mode**: Theme switching
- [ ] **Export Audit Logs**: CSV export of operation history
- [ ] **Advanced Analytics**: Recharts integration for deeper insights
- [ ] **Real-time Workflow Monitoring**: Live GitHub Actions log streaming
- [ ] **Azure Cost Optimization**: Recommendations to reduce costs
- [ ] **Backup & Restore**: Automated Cosmos DB backups
- [ ] **Service Health Monitoring**: Automatic restart on crashes
- [ ] **Log Filtering & Search**: Filter console logs by service, type, or keyword
- [ ] **Service Presets**: Save and load service configurations
- [ ] **Port Conflict Detection**: Warn when ports are already in use

---

## 🛠️ Technology Stack

### Frontend

- **React 18** - UI framework
- **TypeScript 5** - Type safety
- **Vite 5** - Build tool and dev server
- **TailwindCSS 3** - Utility-first CSS
- **Monaco Editor** - Bicep file viewing
- **Tauri API** - Desktop functionality

### Backend

- **Tauri 1.5** - Cross-platform desktop framework
- **Rust** - Native performance
- **Tokio** - Async runtime
- **Serde** - JSON serialization

### Services

- **.NET 9** - Business logic
- **Azure SDK** - Cosmos DB, Blob Storage
- **GitHub CLI** - Workflow automation
- **Azure CLI** - Resource management

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Workflow

1. Read **DEVHUB_ARCHITECTURE.md**
2. Review **SECURITY.md** for security requirements
3. Follow code style guidelines
4. Write tests (future)
5. Update documentation
6. Submit PR with detailed description

---

## 📄 License

[Your License Here]

---

## 🙋 Support

### Getting Help

- **Documentation**: Start with this README and linked docs
- **Configuration Issues**: See [CONFIGURATION.md](./CONFIGURATION.md)
- **Security Questions**: See [SECURITY.md](./SECURITY.md)
- **Bug Reports**: Open GitHub issue with reproduction steps
- **Feature Requests**: Open GitHub issue with use case

### Contact

- **Development Team**: [Contact Info]
- **Security Issues**: [Security Contact] (private disclosure)

---

## 🎉 Acknowledgments

Built with:

- [Tauri](https://tauri.app/) - Desktop framework
- [React](https://react.dev/) - UI library
- [Monaco Editor](https://microsoft.github.io/monaco-editor/) - Code editor
- [TailwindCSS](https://tailwindcss.com/) - CSS framework

---

**Mystira DevHub v0.1.0** - Modern Development Operations for Mystira Application Suite

Made with ❤️ by the Mystira Development Team
