# DevHub Configuration Guide

## Overview

This guide explains how to configure Mystira DevHub for your environment.

## Prerequisites

Before configuring DevHub, ensure you have:

- ✅ .NET 9 SDK installed
- ✅ Node.js 18+ and npm
- ✅ Rust and Cargo installed
- ✅ Azure CLI authenticated (`az login`)
- ✅ GitHub CLI authenticated (`gh auth login`)
- ✅ Access to Azure resources (Cosmos DB, Storage, etc.)
- ✅ Repository permissions for GitHub Actions

## Environment Configuration

### Development Environment

**1. Clone the repository**
```bash
git clone https://github.com/phoenixvc/Mystira.App.git
cd Mystira.App/tools/Mystira.DevHub
```

**2. Install dependencies**
```bash
# Install npm packages
npm install

# Build .NET services
cd ../Mystira.DevHub.Services
dotnet build

# Build .NET CLI
cd ../Mystira.DevHub.CLI
dotnet build
```

**3. Run in development mode**
```bash
cd ../Mystira.DevHub
npm run tauri dev
```

### Connection Strings

DevHub requires connection strings for various operations. You can provide them via:

#### Option 1: Environment Variables (Recommended)

```bash
# Cosmos DB
export SOURCE_COSMOS_CONNECTION="AccountEndpoint=https://..."
export DEST_COSMOS_CONNECTION="AccountEndpoint=https://..."

# Blob Storage
export SOURCE_STORAGE_CONNECTION="DefaultEndpointsProtocol=https;..."
export DEST_STORAGE_CONNECTION="DefaultEndpointsProtocol=https;..."
```

#### Option 2: appsettings.json

Create `tools/Mystira.DevHub.CLI/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "CosmosDb": "AccountEndpoint=...",
    "AzureStorage": "DefaultEndpointsProtocol=..."
  }
}
```

**⚠️ IMPORTANT**: Add `appsettings.json` to `.gitignore` to prevent committing credentials.

#### Option 3: UI Input

Enter connection strings directly in the DevHub UI:
- Migration Manager → Configuration step
- Cosmos Explorer → Connection Manager (future)

### Azure Configuration

**Subscription Setup**
```bash
# List subscriptions
az account list --output table

# Set active subscription
az account set --subscription "Your Subscription Name"

# Verify
az account show
```

**Resource Group**
```bash
# Create development resource group (if needed)
az group create --name dev-rg --location westeurope
```

### GitHub Configuration

**Authentication**
```bash
# Login
gh auth login

# Verify
gh auth status

# Set default repository (optional)
gh repo set-default phoenixvc/Mystira.App
```

**Workflow Permissions**

Ensure GitHub Actions has permissions to:
- Trigger workflows
- Access secrets
- Deploy to Azure

## Feature Configuration

### Cosmos Explorer

**Database Configuration**
- Default database name: `MystiraDb`
- Containers accessed:
  - `Scenarios`
  - `ContentBundles`
  - `MediaAssets`
  - `GameSessions`

**Export Settings**
- Default format: CSV
- Output path: User-selected via file dialog
- Encoding: UTF-8

### Migration Manager

**Default Settings**
```json
{
  "databaseName": "MystiraDb",
  "containerName": "media-assets",
  "batchSize": 100,
  "retryAttempts": 3
}
```

**Migration Types**
- `scenarios`: Cosmos DB Scenarios container
- `bundles`: Cosmos DB ContentBundles container
- `media-metadata`: Cosmos DB MediaAssets container
- `blobs`: Azure Blob Storage files
- `all`: All of the above

### Infrastructure Panel

**Workflow Configuration**
```typescript
const config = {
  workflowFile: 'infrastructure-deploy-dev.yml',
  repository: 'phoenixvc/Mystira.App',
  branch: 'main'
};
```

**Bicep Templates Path**
```
infrastructure/dev/
├── main.bicep
└── modules/
    ├── cosmos-db.bicep
    ├── storage.bicep
    ├── app-service.bicep
    ├── communication-services.bicep
    ├── log-analytics.bicep
    └── application-insights.bicep
```

## Build Configuration

### Development Build

```bash
# Run with hot reload
npm run tauri dev
```

### Production Build

```bash
# Build optimized bundle
npm run tauri build

# Output locations:
# Windows: src-tauri/target/release/bundle/msi/
# macOS: src-tauri/target/release/bundle/dmg/
# Linux: src-tauri/target/release/bundle/deb/ (or rpm/)
```

### Build Options

**Tauri Configuration** (`src-tauri/tauri.conf.json`):

```json
{
  "package": {
    "productName": "Mystira DevHub",
    "version": "0.1.0"
  },
  "build": {
    "distDir": "../dist",
    "devPath": "http://localhost:5173"
  },
  "tauri": {
    "bundle": {
      "identifier": "com.mystira.devhub",
      "icon": [
        "icons/32x32.png",
        "icons/128x128.png",
        "icons/icon.icns",
        "icons/icon.ico"
      ]
    }
  }
}
```

## Application Settings

### UI Preferences

Current version stores preferences in component state (session-only).

Future version will persist to:
- **Windows**: `%APPDATA%\Mystira DevHub\config.json`
- **macOS**: `~/Library/Application Support/Mystira DevHub/config.json`
- **Linux**: `~/.config/mystira-devhub/config.json`

**Settings Schema** (future):
```json
{
  "theme": "light",  // "light" | "dark" | "auto"
  "defaultPaths": {
    "export": "~/Documents/exports",
    "logs": "~/Documents/logs"
  },
  "notifications": {
    "enabled": true,
    "sound": false
  },
  "advanced": {
    "logLevel": "info",
    "autoUpdate": true
  }
}
```

## Troubleshooting Configuration

### .NET CLI Not Found

**Error**: `dotnet: command not found`

**Solution**:
```bash
# Install .NET 9 SDK
# Windows: Download from https://dotnet.microsoft.com/download
# macOS: brew install dotnet-sdk
# Linux: Follow distribution-specific instructions
```

### Azure CLI Not Authenticated

**Error**: `Please run 'az login' to setup account`

**Solution**:
```bash
az login
az account set --subscription "your-subscription"
```

### GitHub CLI Not Authenticated

**Error**: `gh: To get started with GitHub CLI, please run: gh auth login`

**Solution**:
```bash
gh auth login
# Follow interactive prompts
```

### Tauri Build Fails

**Error**: `Error: Failed to build Tauri application`

**Solution**:
```bash
# Install Rust if not already installed
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh

# Update Rust
rustup update

# Install platform-specific dependencies
# Linux: sudo apt install libwebkit2gtk-4.0-dev build-essential curl wget file
# macOS: xcode-select --install
```

### Monaco Editor Not Loading

**Error**: Blank editor or console error about Monaco

**Solution**:
```bash
# Reinstall dependencies
rm -rf node_modules package-lock.json
npm install
```

## Performance Tuning

### Build Performance

```bash
# Use release mode for faster Rust builds
cargo build --release

# Enable parallel npm builds
npm install --prefer-offline --no-audit
```

### Runtime Performance

- **Minimize bundle size**: Tree-shaking enabled by Vite
- **Lazy loading**: Components loaded on demand
- **Memoization**: React components memoized where appropriate

### Database Query Optimization

For large datasets:

```csharp
// Use pagination in custom queries
var query = container.GetItemQueryIterator<T>(
    queryDefinition,
    requestOptions: new QueryRequestOptions
    {
        MaxItemCount = 100  // Batch size
    }
);
```

## Advanced Configuration

### Custom .NET Commands

Extend CLI with new commands:

**1. Add service method** (`Mystira.DevHub.Services`):
```csharp
public interface IMyService
{
    Task<Result> MyOperationAsync();
}
```

**2. Add CLI command** (`Mystira.DevHub.CLI`):
```csharp
public class MyCommands
{
    public async Task<CommandResponse> MyOperationAsync(JsonElement args)
    {
        // Implementation
    }
}
```

**3. Add Tauri command** (`src-tauri/src/main.rs`):
```rust
#[tauri::command]
async fn my_operation() -> Result<CommandResponse, String> {
    execute_devhub_cli("my.operation".to_string(), json!({})).await
}
```

**4. Add UI component** (`src/components/MyFeature.tsx`):
```typescript
const response = await invoke('my_operation');
```

### Custom Bicep Templates

Add templates to be viewable in DevHub:

**1. Place Bicep file** in `infrastructure/dev/modules/`

**2. Update file tree** in `BicepViewer.tsx`:
```typescript
const BICEP_FILES: BicepFile[] = [
  // ... existing files
  {
    name: 'my-resource.bicep',
    path: 'infrastructure/dev/modules/my-resource.bicep',
    type: 'file',
  },
];
```

## Configuration Validation

**Pre-flight checklist**:

```bash
# Verify all tools installed
dotnet --version      # Should be 9.0.x
node --version        # Should be 18+
npm --version
cargo --version
gh --version
az --version

# Verify authentication
gh auth status        # Should show "Logged in"
az account show       # Should show active subscription

# Verify builds
cd tools/Mystira.DevHub.Services && dotnet build
cd ../Mystira.DevHub.CLI && dotnet build
cd ../Mystira.DevHub && npm run build
```

## Migration from CosmosConsole

If migrating from the legacy `CosmosConsole` tool:

1. **Export existing configurations**:
   - Connection strings
   - Frequently used commands
   - Custom scripts

2. **Map to DevHub features**:
   - `console export` → Cosmos Explorer → Export Panel
   - `console stats` → Cosmos Explorer → Statistics Panel
   - `console migrate` → Migration Manager

3. **Update scripts/workflows**:
   - Replace console commands with DevHub operations
   - Use GitHub Actions for automation instead of cron jobs

## Getting Help

### Documentation

- **Architecture**: `docs/architecture/DEVHUB_ARCHITECTURE.md`
- **Security**: `tools/Mystira.DevHub/SECURITY.md`
- **README**: `tools/Mystira.DevHub/README.md`

### Support Channels

- GitHub Issues: Bug reports and feature requests
- Development Team: Technical questions
- Architecture Review: Design discussions

---

**Document Version**: 1.0
**Last Updated**: 2025-11-23
**Maintainer**: Development Team
