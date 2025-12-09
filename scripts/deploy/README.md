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

### StaticWebAppHelpers.psm1

Static Web App management functions:

- `Get-StaticWebAppSupportedRegions` - Get list of regions where SWA is available
- `Test-StaticWebAppRegion` - Check if a region supports Static Web Apps
- `Get-StaticWebAppFallbackRegion` - Get fallback region if requested region is unsupported
- `Get-GitHubRepositoryInfo` - Extract GitHub repo owner and name from git remote URL
- `Test-StaticWebAppExists` - Check if a Static Web App exists
- `Connect-StaticWebAppToGitHub` - Connect SWA to GitHub repository via REST API

### SecretHelpers.psm1

Secret management functions:

- `Get-ExistingJwtSecret` - Retrieve existing JWT secret from App Service app settings
- `New-JwtSecret` - Generate a new JWT secret key

### GitHelpers.psm1

Git repository operations:

- `Get-GitRepositoryStatus` - Get current git repository status
- `Commit-GitChanges` - Commit changes to git repository
- `Push-GitBranch` - Push a branch to remote
- `Get-GitRemoteUrl` - Get the remote URL for a git repository
- `Sync-GitRepository` - Fetch latest changes and ensure branch is up to date

### ResourceInfoHelpers.psm1

Resource detail retrieval functions:

- `Get-AppServiceInfo` - Get detailed information about an App Service
- `Get-CosmosDbInfo` - Get detailed information about a Cosmos DB account
- `Get-StorageAccountInfo` - Get detailed information about a Storage Account
- `Get-StaticWebAppInfo` - Get detailed information about a Static Web App
- `Get-ResourceGroupResources` - Get all resources in a resource group
- `Get-ResourcesByType` - Get resources of a specific type in a resource group

### ResourceGroupHelpers.psm1

Resource group operations:

- `Test-ResourceGroupExists` - Check if a resource group exists
- `New-ResourceGroup` - Create a new resource group
- `Get-ResourceGroupInfo` - Get information about a resource group

### BicepHelpers.psm1

Bicep template operations:

- `New-BicepParameterFile` - Create a Bicep parameter file with specified parameters
- `Find-BicepTemplate` - Find a Bicep template file in common locations
- `Invoke-BicepDeployment` - Execute a Bicep deployment

### ResourceSummaryHelpers.psm1

Resource summary display:

- `Show-ResourceSummary` - Display a summary of all resources in a resource group

## Usage

These modules are automatically imported by `.deploy-now.ps1`. They can also be imported manually:

```powershell
Import-Module .\scripts\deploy\AzureHelpers.psm1
Import-Module .\scripts\deploy\ResourceHelpers.psm1
Import-Module .\scripts\deploy\ErrorHandlers.psm1
Import-Module .\scripts\deploy\ErrorFormatter.psm1
Import-Module .\scripts\deploy\RollbackHelpers.psm1
Import-Module .\scripts\deploy\RetryHelpers.psm1
Import-Module .\scripts\deploy\StaticWebAppHelpers.psm1
Import-Module .\scripts\deploy\SecretHelpers.psm1
Import-Module .\scripts\deploy\GitHelpers.psm1
Import-Module .\scripts\deploy\ResourceInfoHelpers.psm1
Import-Module .\scripts\deploy\ResourceGroupHelpers.psm1
Import-Module .\scripts\deploy\BicepHelpers.psm1
Import-Module .\scripts\deploy\ResourceSummaryHelpers.psm1
```

