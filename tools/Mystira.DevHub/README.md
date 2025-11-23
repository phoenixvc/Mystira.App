# Mystira DevHub

A cross-platform desktop application for Mystira development operations, built with Tauri, React, and TypeScript.

## Features

✅ **Infrastructure Control Panel**:
- Validate Bicep templates
- Preview infrastructure changes (what-if)
- Deploy infrastructure via GitHub Actions
- Destroy infrastructure (with confirmation)
- Real-time workflow status monitoring

🔜 **Coming Soon**:
- Cosmos Explorer (export sessions, view statistics)
- Migration Manager (migrate data between environments)
- Dashboard (quick actions, connection status)

## Architecture

```
React Frontend (TypeScript)
     ↓
Tauri (Rust Backend)
     ↓
Spawns .NET CLI Process
     ↓
DevHub.Services (C#)
     ↓
Azure / GitHub / Cosmos DB
```

## Prerequisites

### Required
- **Node.js 18+** and npm
- **Rust** and Cargo: https://rustup.rs/
- **Tauri CLI**: `cargo install tauri-cli`
- **.NET 9 SDK**: For the CLI backend
- **GitHub CLI**: `gh` (for infrastructure operations)
- **Azure CLI**: `az` (for what-if analysis)

### Authentication
Before using the app:
- `gh auth login` - Authenticate with GitHub
- `az login` - Authenticate with Azure

## Getting Started

### 1. Install Dependencies

```bash
cd tools/Mystira.DevHub
npm install
```

### 2. Run in Development Mode

```bash
npm run tauri:dev
```

This will:
- Start the Vite dev server for React
- Compile the Rust backend
- Launch the Tauri application
- Enable hot-reload for frontend changes

### 3. Build for Production

```bash
npm run tauri:build
```

This creates platform-specific installers in `src-tauri/target/release/bundle/`.

## Project Structure

```
Mystira.DevHub/
├── src/                          # React frontend
│   ├── components/
│   │   └── InfrastructurePanel.tsx
│   ├── services/                 # Tauri API wrappers (future)
│   ├── App.tsx
│   ├── main.tsx
│   └── index.css
├── src-tauri/                    # Rust backend
│   ├── src/
│   │   └── main.rs               # Tauri commands
│   ├── Cargo.toml
│   ├── tauri.conf.json
│   └── build.rs
├── public/                       # Static assets
├── package.json
├── vite.config.ts
├── tsconfig.json
├── tailwind.config.js
└── README.md
```

## Available Commands

### Infrastructure Operations

All commands are accessible via the UI buttons:

- **🔍 Validate**: Checks Bicep template syntax and ARM validation
- **👁️ Preview**: Runs what-if analysis to show resource changes
- **🚀 Deploy**: Triggers GitHub Actions workflow to deploy infrastructure
- **💥 Destroy**: Deletes all infrastructure (requires typing "DELETE")

Each operation:
1. Sends command to Rust backend
2. Rust spawns .NET CLI process
3. .NET CLI calls the appropriate service
4. Service executes via GitHub CLI or Azure CLI
5. Response flows back to UI

## Configuration

### Tauri Configuration

`src-tauri/tauri.conf.json` controls:
- Window size and behavior
- Permissions (filesystem, shell execution, dialogs)
- Build settings
- Application metadata

### Environment Variables

Set these for development:
- `COSMOS_CONNECTION_STRING` - For Cosmos operations
- `SOURCE_COSMOS_CONNECTION` - For migrations
- `DEST_COSMOS_CONNECTION` - For migrations

## Development Tips

### Hot Reload
- Frontend changes (React/TypeScript) reload automatically
- Rust backend changes require recompilation (automatic in dev mode)
- .NET CLI changes require rebuilding the CLI project

### Debugging

**Frontend**:
- Open DevTools: Right-click → Inspect Element
- Console logs appear in DevTools

**Rust Backend**:
- Logs appear in terminal where `tauri:dev` was run
- Use `println!()` for debugging

**.NET CLI**:
- Errors appear in the app's response boxes
- Check `stdout`/`stderr` from the spawned process

### Testing Infrastructure Commands

Without running the full Tauri app, you can test the .NET CLI directly:

```bash
echo '{"command":"infrastructure.validate","args":{"workflowFile":"infrastructure-deploy-dev.yml","repository":"phoenixvc/Mystira.App"}}' | \
  dotnet run --project ../Mystira.DevHub.CLI
```

## Technology Stack

### Frontend
- **React 18** - UI framework
- **TypeScript** - Type safety
- **Vite** - Build tool and dev server
- **TailwindCSS** - Utility-first CSS
- **Tauri API** - Bridge to Rust backend

### Backend
- **Tauri 1.5** - Desktop framework (Rust)
- **Tokio** - Async runtime
- **Serde** - JSON serialization

### Services
- **.NET 9** - Business logic
- **Azure SDK** - Cosmos DB, Blob Storage
- **GitHub CLI** - Workflow triggers
- **Azure CLI** - Infrastructure validation

## Building for Distribution

### Windows
```bash
npm run tauri:build
```
Creates: `src-tauri/target/release/bundle/msi/Mystira DevHub_0.1.0_x64_en-US.msi`

### macOS
```bash
npm run tauri:build
```
Creates: `src-tauri/target/release/bundle/dmg/Mystira DevHub_0.1.0_x64.dmg`

### Linux
```bash
npm run tauri:build
```
Creates:
- `src-tauri/target/release/bundle/deb/mystira-devhub_0.1.0_amd64.deb`
- `src-tauri/target/release/bundle/appimage/mystira-devhub_0.1.0_amd64.AppImage`

## Security

### Permissions
The app uses Tauri's permission system:
- **Shell execute**: Required to spawn .NET CLI processes
- **Filesystem**: Read/write for CSV exports and configuration
- **Dialog**: For file pickers and confirmations

### Secrets
- Never hardcoded in source code
- Use environment variables or Azure Key Vault
- .NET CLI inherits environment from Tauri process

### GitHub/Azure Authentication
- Uses existing authenticated CLI sessions
- No token storage in the app
- Respects user's current auth context

## Troubleshooting

### "dotnet: command not found"
Install .NET 9 SDK: https://dotnet.microsoft.com/download

### "gh: command not found"
Install GitHub CLI: https://cli.github.com/

### "Failed to spawn dotnet process"
Check that:
1. .NET 9 SDK is installed: `dotnet --version`
2. DevHub.CLI project exists at `../Mystira.DevHub.CLI`
3. Path in `main.rs` is correct (adjust if needed)

### GitHub Actions workflow not triggering
Ensure:
1. Authenticated: `gh auth status`
2. Have access to repository
3. Workflow file exists in `.github/workflows/`

### Infrastructure commands fail
Check:
1. GitHub CLI is authenticated
2. Azure CLI is authenticated (for what-if)
3. Have required permissions on Azure subscription
4. Workflow secrets are configured in GitHub repository

## Contributing

### Adding New Commands

1. **Add Tauri command** in `src-tauri/src/main.rs`:
```rust
#[tauri::command]
async fn my_new_command(param: String) -> Result<CommandResponse, String> {
    execute_devhub_cli("my.command".to_string(), serde_json::json!({
        "param": param
    })).await
}
```

2. **Register command** in `main()`:
```rust
.invoke_handler(tauri::generate_handler![
    my_new_command,
    // ... existing commands
])
```

3. **Add .NET CLI handler** in `Mystira.DevHub.CLI/Commands/`:
```csharp
public async Task<CommandResponse> MyCommandAsync(JsonElement argsJson) {
    // Implementation
}
```

4. **Route in CLI** in `Program.cs`:
```csharp
"my.command" => await myCommands.MyCommandAsync(request.Args),
```

5. **Call from React**:
```typescript
const response = await invoke('my_new_command', { param: 'value' });
```

### Adding New UI Components

1. Create component in `src/components/`
2. Import and use in `App.tsx`
3. Add navigation button in sidebar
4. Style with TailwindCSS

## Roadmap

### Phase 2 (Current) ✅
- [x] Tauri application scaffolding
- [x] React + TypeScript setup
- [x] Infrastructure Control Panel
- [x] Rust → .NET CLI integration

### Phase 3 (Next)
- [ ] Cosmos Explorer UI
- [ ] Export sessions to CSV
- [ ] View scenario statistics with charts

### Phase 4
- [ ] Migration Manager UI
- [ ] Visual connection configuration
- [ ] Real-time migration progress

### Phase 5
- [ ] Dashboard with quick actions
- [ ] Connection status indicators
- [ ] Recent operations log

### Phase 6
- [ ] Monaco Editor for Bicep viewing
- [ ] Azure resource status grid
- [ ] Deployment history timeline

## License

[Your License Here]

## Support

For questions or issues:
1. Check this README
2. Review documentation in `docs/architecture/DEVHUB_ARCHITECTURE.md`
3. Check CLI README: `../Mystira.DevHub.CLI/README.md`
4. Review the roadmap: `../DEVHUB_IMPLEMENTATION_ROADMAP.md`
