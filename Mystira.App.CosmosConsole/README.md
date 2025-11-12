# Mystira.App.CosmosConsole

A console application for interfacing with Cosmos DB to generate reports and statistics for the Mystira application.

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

## Requirements

- .NET 8.0 SDK
- Access to Azure Cosmos DB account
- Valid Cosmos DB connection string
- Appropriate permissions to read GameSessions, Accounts, and Scenarios containers

## Security Notes

- Store Cosmos DB connection strings securely
- Use Azure AD authentication where possible
- Never commit connection strings to source control
- Ensure least privilege access for the Cosmos DB account