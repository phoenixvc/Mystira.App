# Deployment Script Modules

This directory contains PowerShell modules used by `.deploy-now.ps1`.

## Modules

### AzureHelpers.psm1

Azure CLI helper functions with timeout protection:

- `Invoke-AzCliWithTimeout` - Execute Azure CLI commands with timeout
- `Test-AzureLogin` - Check if user is logged into Azure
- `Set-AzureSubscription` - Set the active Azure subscription

### ResourceHelpers.psm1

Resource scanning and naming functions:

- `Get-ResourceGroupName` - Get resource group name for a location
- `Get-StaticWebAppName` - Get Static Web App name for a location
- `Get-ResourcePrefix` - Get resource prefix for a location
- `Get-ExistingResources` - Scan for existing Azure resources
- `Test-Region` - Validate region is in supported list

### ErrorHandlers.psm1

Deployment error handling functions:

- `Handle-AppServiceConflict` - Handle App Service conflicts
- `Handle-StorageConflict` - Handle storage account conflicts
- `Handle-CommunicationServiceConflict` - Handle Communication Service conflicts
- `Handle-CosmosDbConflict` - Handle Cosmos DB conflicts

### ErrorFormatter.psm1

Error message formatting functions:

- `Format-Error` - Format error messages consistently
- `Format-AzureError` - Format Azure-specific error messages

### RollbackHelpers.psm1

Rollback and resource tracking functions:

- `Initialize-RollbackTracking` - Initialize rollback tracking for a deployment
- `Register-Resource` - Register a created resource
- `Get-CreatedResources` - Get list of created resources
- `Invoke-Rollback` - Rollback created resources

### RetryHelpers.psm1

Transient error retry functions:

- `Test-TransientError` - Determine if an error is transient
- `Invoke-WithRetry` - Execute a command with retry logic
- `Invoke-AzureCliWithRetry` - Execute Azure CLI command with retry

## Usage

These modules are automatically imported by `.deploy-now.ps1`. They can also be imported manually:

```powershell
Import-Module .\scripts\deploy\AzureHelpers.psm1
Import-Module .\scripts\deploy\ResourceHelpers.psm1
Import-Module .\scripts\deploy\ErrorHandlers.psm1
Import-Module .\scripts\deploy\ErrorFormatter.psm1
Import-Module .\scripts\deploy\RollbackHelpers.psm1
Import-Module .\scripts\deploy\RetryHelpers.psm1
```

