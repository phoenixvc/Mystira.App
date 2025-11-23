# Mystira.App.CosmosConsole

A console application for interfacing with Cosmos DB to generate reports, statistics, and migrate data between environments for the Mystira application.

## Features

### Export Game Sessions to CSV
Exports all game sessions from the Cosmos DB, joined with account information to extract user email and alias.

**Usage:**
```bash
Mystira.App.CosmosConsole export --output sessions.csv
```

**Output CSV columns:**
- SessionId: Unique identifier for the game session
- ScenarioId: ID of the scenario played
- ScenarioName: Name/title of the scenario
- AccountId: Account ID of the user
- AccountEmail: Email address of the user account
- AccountAlias: Display name/alias of the user account
- ProfileId: Profile ID used in the session
- StartedAt: Date and time when the session started
- IsCompleted: Boolean indicating if the session was completed
- CompletedAt: Date and time when the session was completed (null if not completed)

### Scenario Statistics
Shows completion statistics for each scenario, including per-account breakdowns.

**Usage:**
```bash
Mystira.App.CosmosConsole stats
```

**Output includes:**
- Total sessions per scenario
- Number of completed sessions per scenario
- Completion rate (percentage)
- Per-account breakdown showing:
  - Individual session counts
  - Individual completion counts
  - Per-account completion rates

### Data Migration (NEW)
Migrate data from old resource naming to new standardized naming convention, or between environments.

**Usage:**
```bash
# Show migration help
Mystira.App.CosmosConsole migrate --help

# Migrate scenarios
Mystira.App.CosmosConsole migrate scenarios

# Migrate content bundles
Mystira.App.CosmosConsole migrate bundles

# Migrate media assets metadata
Mystira.App.CosmosConsole migrate media-metadata

# Migrate blob storage files
Mystira.App.CosmosConsole migrate blobs

# Migrate everything
Mystira.App.CosmosConsole migrate all
```

**What gets migrated:**
- **Scenarios**: All scenario definitions and content
- **Content Bundles**: Bundle configurations linking scenarios
- **Media Assets Metadata**: Media asset references (URLs, metadata)
- **Blob Storage**: Actual media files (images, audio, video)

## Configuration

The application requires configuration in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "CosmosDb": "AccountEndpoint=https://your-cosmos-account.documents.azure.com:443/;AccountKey=your-account-key;"
  },
  "Database": {
    "Name": "MystiraAppDb"
  }
}
```

### Getting Cosmos DB Connection String

1. Navigate to your Azure Portal
2. Go to your Cosmos DB account
3. Select "Keys" from the left menu
4. Copy either the PRIMARY KEY or SECONDARY KEY
5. Replace the placeholder values in the appsettings.json

## Building

```bash
dotnet build
```

## Running

The console application supports two main commands:

### Export Command
```bash
# Export all game sessions with account data to CSV
Mystira.App.CosmosConsole export --output path/to/sessions.csv
```

### Statistics Command
```bash
# Show scenario completion statistics
Mystira.App.CosmosConsole stats
```

### Migration Commands
```bash
# Migrate scenarios from old to new Cosmos DB
Mystira.App.CosmosConsole migrate scenarios

# Migrate content bundles
Mystira.App.CosmosConsole migrate bundles

# Migrate media assets metadata
Mystira.App.CosmosConsole migrate media-metadata

# Migrate blob storage files (media assets)
Mystira.App.CosmosConsole migrate blobs

# Migrate everything
Mystira.App.CosmosConsole migrate all
```

## Migration Guide

### Setting Up Migration

Migrations require source and destination connection strings. Set them via environment variables:

**For Cosmos DB migrations:**
```bash
export SOURCE_COSMOS_CONNECTION="AccountEndpoint=https://old-cosmos.documents.azure.com:443/;AccountKey=..."
export DEST_COSMOS_CONNECTION="AccountEndpoint=https://new-cosmos.documents.azure.com:443/;AccountKey=..."
export COSMOS_DATABASE_NAME="MystiraAppDb"  # Optional, defaults to MystiraAppDb
```

**For Blob Storage migration:**
```bash
export SOURCE_STORAGE_CONNECTION="DefaultEndpointsProtocol=https;AccountName=oldaccount;..."
export DEST_STORAGE_CONNECTION="DefaultEndpointsProtocol=https;AccountName=newaccount;..."
export STORAGE_CONTAINER_NAME="mystira-app-media"  # Optional
```

### Migration Example: Old to New Naming Convention

```bash
# Example: Migrating from old naming to standardized naming

# Old resources:
#   Cosmos DB: mystiraappdevcosmos
#   Storage: mystiraappdevstorage

# New resources:
#   Cosmos DB: dev-euw-cosmos-mystira
#   Storage: deveuwstmystira

# Step 1: Get connection strings from Azure Portal
# For Cosmos DB: Go to Keys section
# For Storage: Go to Access keys section

# Step 2: Set environment variables
export SOURCE_COSMOS_CONNECTION="AccountEndpoint=https://mystiraappdevcosmos.documents.azure.com:443/;AccountKey=YOUR_KEY;"
export DEST_COSMOS_CONNECTION="AccountEndpoint=https://dev-euw-cosmos-mystira.documents.azure.com:443/;AccountKey=YOUR_KEY;"
export SOURCE_STORAGE_CONNECTION="DefaultEndpointsProtocol=https;AccountName=mystiraappdevstorage;AccountKey=YOUR_KEY;..."
export DEST_STORAGE_CONNECTION="DefaultEndpointsProtocol=https;AccountName=deveuwstmystira;AccountKey=YOUR_KEY;..."

# Step 3: Run migration
dotnet run -- migrate all

# Or migrate specific resources:
dotnet run -- migrate scenarios
dotnet run -- migrate bundles
dotnet run -- migrate blobs
```

### Migration Features

- **Idempotent**: Safe to run multiple times, uses upsert operations
- **Container Creation**: Automatically creates destination containers if needed
- **Error Handling**: Reports failures per-item, continues processing
- **Progress Tracking**: Shows counts and duration for each migration
- **Server-Side Copy**: Blob migration uses Azure server-side copy (no download/upload)

## Implementation Details

### Architecture
- **Dependency Injection**: Uses Microsoft.Extensions.DependencyInjection for service management
- **Entity Framework Core**: Uses EF Core with Cosmos DB provider
- **CSV Export**: Uses CsvHelper library for CSV generation
- **Configuration**: Uses Microsoft.Extensions.Configuration for app settings
- **Logging**: Uses Microsoft.Extensions.Logging for structured logging

### Data Models
The console uses the same domain models as the main application:
- `GameSession`: Game session data with completion status
- `Account`: User account information with email and display name
- `Scenario`: Scenario information for reporting
- `SessionStatus`: Enum for session completion status

### Error Handling
- Comprehensive error handling with detailed logging
- User-friendly error messages
- Graceful handling of missing configuration or connection issues

## Example Output

### CSV Export Example
```csv
SessionId,ScenarioId,ScenarioName,AccountId,AccountEmail,AccountAlias,ProfileId,StartedAt,IsCompleted,CompletedAt
abc123,scenario1,The Dragon's Quest,user123,dragon@adventure.com,Dragon Master,profile456,2023-11-15T10:30:00Z,True,2023-11-15T11:45:00Z
def456,scenario2,The Lost Kingdom,user123,dragon@adventure.com,Dragon Master,profile789,2023-11-14T14:20:00Z,False,
```

### Statistics Output Example
```
Scenario Completion Statistics:
================================

Scenario: The Dragon's Quest
  Total Sessions: 25
  Completed Sessions: 20
  Completion Rate: 80.0%
  Account Breakdown:
    dragon@adventure.com (Dragon Master):
      Sessions: 15
      Completed: 12
      Completion Rate: 80.0%
    wizard@adventure.com (Spell Caster):
      Sessions: 10
      Completed: 8
      Completion Rate: 80.0%

Scenario: The Lost Kingdom
  Total Sessions: 18
  Completed Sessions: 9
  Completion Rate: 50.0%
  Account Breakdown:
    dragon@adventure.com (Dragon Master):
      Sessions: 12
      Completed: 6
      Completion Rate: 50.0%
    wizard@adventure.com (Spell Caster):
      Sessions: 6
      Completed: 3
      Completion Rate: 50.0%

================================
```

### Infrastructure Deployment (NEW)
Trigger infrastructure deployment workflows directly from the console using GitHub CLI.

**Prerequisites:**
- Install GitHub CLI: https://cli.github.com/
- Authenticate with: `gh auth login`
- Ensure you have access to the repository

**Usage:**
```bash
# Show infrastructure help
dotnet run -- infrastructure --help

# Validate Bicep templates
dotnet run -- infrastructure validate

# Preview infrastructure changes (what-if analysis)
dotnet run -- infrastructure preview

# Deploy infrastructure to Azure
dotnet run -- infrastructure deploy
```

**Features:**
- ✅ Triggers GitHub Actions workflows remotely
- ✅ Validate Bicep templates before deployment
- ✅ Preview infrastructure changes with what-if analysis
- ✅ Deploy infrastructure changes
- ✅ View workflow status and progress

**Output:**
- Workflow trigger confirmation
- Commands to view workflow run status
- Commands to watch workflow progress in real-time

**Example:**
```bash
$ dotnet run -- infrastructure deploy
🚀 Triggering infrastructure deployment workflow with action: deploy
This will dispatch the 'Infrastructure Deployment - Dev Environment' workflow...

✅ Workflow triggered successfully!

📊 View workflow run:
   gh run list --workflow=infrastructure-deploy-dev.yml --limit 1

🔍 Watch workflow progress:
   gh run watch
```

## Requirements

- .NET 9.0 SDK
- Access to Azure Cosmos DB account
- Valid Cosmos DB connection string
- Appropriate permissions to read GameSessions, Accounts, and Scenarios containers
- GitHub CLI (gh) for infrastructure deployment features

## Security Notes

- Store Cosmos DB connection strings securely
- Use Azure AD authentication where possible
- Never commit connection strings to source control
- GitHub CLI respects your authentication and repository access permissions
- Ensure least privilege access for the Cosmos DB account