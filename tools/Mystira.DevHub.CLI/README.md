# Mystira.DevHub.CLI

A JSON-based CLI wrapper for DevHub services. Designed to be called from Tauri's Rust backend.

## Architecture

```
Tauri (Rust) → Spawns Process → DevHub.CLI
                                    ↓
                            Reads JSON from stdin
                                    ↓
                            Routes to Services
                                    ↓
                        Returns JSON to stdout
```

## Usage

### Input Format (stdin)

```json
{
  "command": "cosmos.export",
  "args": {
    "outputPath": "/path/to/output.csv"
  }
}
```

### Output Format (stdout)

```json
{
  "success": true,
  "result": {
    "rowCount": 150,
    "outputPath": "/path/to/output.csv"
  },
  "message": "Exported 150 sessions to /path/to/output.csv",
  "error": null
}
```

## Available Commands

### Cosmos Operations

#### cosmos.export
Export game sessions to CSV

```json
{
  "command": "cosmos.export",
  "args": {
    "outputPath": "/path/to/sessions.csv"
  }
}
```

#### cosmos.stats
Get scenario statistics

```json
{
  "command": "cosmos.stats",
  "args": {}
}
```

### Migration Operations

#### migration.run
Run data migrations

```json
{
  "command": "migration.run",
  "args": {
    "type": "all",
    "sourceCosmosConnection": "AccountEndpoint=...",
    "destCosmosConnection": "AccountEndpoint=...",
    "sourceStorageConnection": "DefaultEndpointsProtocol=...",
    "destStorageConnection": "DefaultEndpointsProtocol=...",
    "databaseName": "MystiraAppDb",
    "containerName": "mystira-app-media"
  }
}
```

**Migration Types**:
- `scenarios` - Migrate scenarios only
- `bundles` - Migrate content bundles only
- `media-metadata` - Migrate media assets metadata only
- `blobs` - Migrate blob storage only
- `all` - Migrate everything

### Infrastructure Operations

#### infrastructure.validate
Validate Bicep templates

```json
{
  "command": "infrastructure.validate",
  "args": {
    "workflowFile": "infrastructure-deploy-dev.yml",
    "repository": "phoenixvc/Mystira.App"
  }
}
```

#### infrastructure.preview
Preview infrastructure changes (what-if)

```json
{
  "command": "infrastructure.preview",
  "args": {
    "workflowFile": "infrastructure-deploy-dev.yml",
    "repository": "phoenixvc/Mystira.App"
  }
}
```

#### infrastructure.deploy
Deploy infrastructure

```json
{
  "command": "infrastructure.deploy",
  "args": {
    "workflowFile": "infrastructure-deploy-dev.yml",
    "repository": "phoenixvc/Mystira.App"
  }
}
```

#### infrastructure.destroy
Destroy infrastructure (requires confirmation)

```json
{
  "command": "infrastructure.destroy",
  "args": {
    "workflowFile": "infrastructure-deploy-dev.yml",
    "repository": "phoenixvc/Mystira.App",
    "confirm": true
  }
}
```

#### infrastructure.status
Get workflow status

```json
{
  "command": "infrastructure.status",
  "args": {
    "workflowFile": "infrastructure-deploy-dev.yml",
    "repository": "phoenixvc/Mystira.App"
  }
}
```

## Configuration

### Connection Strings

Connection strings can be provided in multiple ways (in order of precedence):

1. **Command arguments** (highest priority)
2. **Environment variables**:
   - `COSMOS_CONNECTION_STRING`
   - `SOURCE_COSMOS_CONNECTION`
   - `DEST_COSMOS_CONNECTION`
   - `SOURCE_STORAGE_CONNECTION`
   - `DEST_STORAGE_CONNECTION`
3. **appsettings.json** (lowest priority, not recommended for secrets)

### Example Environment Variables

```bash
export COSMOS_CONNECTION_STRING="AccountEndpoint=https://..."
export SOURCE_COSMOS_CONNECTION="AccountEndpoint=https://old..."
export DEST_COSMOS_CONNECTION="AccountEndpoint=https://new..."
```

## Building

```bash
cd tools/Mystira.DevHub.CLI
dotnet build
```

## Testing

### Test from command line

```bash
# Export sessions
echo '{"command":"cosmos.export","args":{"outputPath":"./sessions.csv"}}' | \
  dotnet run --project tools/Mystira.DevHub.CLI

# Get statistics
echo '{"command":"cosmos.stats","args":{}}' | \
  dotnet run --project tools/Mystira.DevHub.CLI

# Validate infrastructure
echo '{"command":"infrastructure.validate","args":{"workflowFile":"infrastructure-deploy-dev.yml","repository":"phoenixvc/Mystira.App"}}' | \
  dotnet run --project tools/Mystira.DevHub.CLI
```

## Error Handling

All errors are returned as JSON with `success: false`:

```json
{
  "success": false,
  "result": null,
  "message": "Source and destination Cosmos DB connection strings are required",
  "error": "ArgumentException: Missing required parameter"
}
```

## Integration with Tauri

From Rust, spawn the CLI process and communicate via stdin/stdout:

```rust
use std::process::{Command, Stdio};
use serde_json::json;

async fn call_devhub_cli(command: &str, args: serde_json::Value) -> Result<serde_json::Value, String> {
    let input = json!({
        "command": command,
        "args": args
    });

    let mut child = Command::new("dotnet")
        .arg("run")
        .arg("--project")
        .arg("tools/Mystira.DevHub.CLI/Mystira.DevHub.CLI.csproj")
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .spawn()
        .map_err(|e| e.to_string())?;

    // Write JSON to stdin
    let stdin = child.stdin.as_mut().ok_or("Failed to get stdin")?;
    serde_json::to_writer(stdin, &input).map_err(|e| e.to_string())?;

    // Read JSON from stdout
    let output = child.wait_with_output().await.map_err(|e| e.to_string())?;
    let response: serde_json::Value = serde_json::from_slice(&output.stdout)
        .map_err(|e| e.to_string())?;

    Ok(response)
}
```

## Logging

The CLI uses minimal console logging (errors only) to avoid polluting the JSON output. All other logs go to the configured logging providers.

## Security

- **Never** commit connection strings to version control
- Use environment variables or Azure Key Vault for secrets
- The CLI wrapper itself doesn't store any credentials
- All authentication happens through the underlying services (Azure SDK, GitHub CLI)

## Dependencies

- .NET 9 SDK
- Mystira.DevHub.Services library
- Azure SDK (for Cosmos DB and Blob Storage operations)
- GitHub CLI (`gh`) for infrastructure operations
