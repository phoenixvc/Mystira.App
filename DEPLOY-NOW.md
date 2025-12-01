# Deploy Now Script - Complete Documentation

## Overview

The `.deploy-now.ps1` script is a smart deployment tool that automatically determines whether to deploy infrastructure or push code based on the current state of Azure resources. It handles resource conflicts, provides rollback capabilities, and includes comprehensive error handling.

## Features

### ✅ Smart Deployment Detection

- Automatically detects if infrastructure exists
- Deploys infrastructure if missing
- Triggers CI/CD if infrastructure exists

### ✅ Conflict Resolution

- Handles storage account conflicts
- Resolves Communication Service conflicts
- Manages Cosmos DB region/name conflicts
- Handles App Service conflicts

### ✅ Rollback Support

- Tracks created resources during deployment
- Offers automatic cleanup on failure
- Prevents orphaned resources

### ✅ Transient Error Retry

- Automatically retries transient failures (network, rate limits, timeouts)
- Exponential backoff for retries
- Smart error detection

### ✅ Logging & Monitoring

- Optional log file with `-LogPath`
- Progress indicators during deployment
- Detailed resource summaries

### ✅ Safety Features

- Dry-run mode with `-WhatIf`
- Input validation
- Git repository validation
- Region validation

## Usage

### Basic Usage

```powershell
# Deploy to default region (southafricanorth)
.\.deploy-now.ps1

# Deploy to specific region
.\.deploy-now.ps1 -Region "westeurope"

# Deploy with logging
.\.deploy-now.ps1 -Region "southafricanorth" -LogPath ".\deploy.log"

# Preview deployment (dry-run)
.\.deploy-now.ps1 -Region "southafricanorth" -WhatIf

# Skip resource scanning (faster startup)
.\.deploy-now.ps1 -Region "southafricanorth" -SkipScan

# Verbose output
.\.deploy-now.ps1 -Region "southafricanorth" -Verbose

# Custom subscription
.\.deploy-now.ps1 -Region "southafricanorth" -SubscriptionId "your-sub-id"
```

### Advanced Usage

```powershell
# All options combined
.\.deploy-now.ps1 `
    -Region "southafricanorth" `
    -SubscriptionId "your-sub-id" `
    -LogPath ".\deploy.log" `
    -WhatIf `
    -Verbose `
    -SkipScan
```

## Parameters

| Parameter         | Type   | Default                                | Description                               |
| ----------------- | ------ | -------------------------------------- | ----------------------------------------- |
| `-Region`         | String | `southafricanorth`                     | Azure region for deployment               |
| `-Branch`         | String | Current branch                         | Git branch for code deployment            |
| `-Message`        | String | `"Trigger deployment"`                 | Commit message for code deployment        |
| `-Verbose`        | Switch | `$false`                               | Show detailed debug output                |
| `-SkipScan`       | Switch | `$false`                               | Skip resource scanning for faster startup |
| `-WhatIf`         | Switch | `$false`                               | Preview deployment without executing      |
| `-SubscriptionId` | String | `22f9eb18-6553-4b7d-9451-47d0195085fe` | Azure subscription ID                     |
| `-LogPath`        | String | `""`                                   | Path to log file (optional)               |

## Supported Regions

- `southafricanorth` (default)
- `eastus2`
- `westus2`
- `centralus`
- `westeurope`
- `northeurope`
- `eastasia`

## Module Structure

The script uses a modular architecture with helper modules:

``` text
scripts/deploy/
├── AzureHelpers.psm1      # Azure CLI timeout protection, login
├── ResourceHelpers.psm1   # Resource scanning, naming, validation
├── ErrorHandlers.psm1     # Deployment error handling
├── ErrorFormatter.psm1   # Error message formatting
├── RollbackHelpers.psm1  # Rollback and resource tracking
├── RetryHelpers.psm1     # Transient error retry logic
└── README.md             # Module documentation
```

## Conflict Resolution

### Storage Account Conflicts

When a storage account name is already taken:

- **Option 1**: Use existing storage account (retrieves connection string)
- **Option 2**: Delete existing storage account and recreate

### Communication Service Conflicts

When a Communication Service name is taken:

- **Option 1**: Switch to `eastus2` region
- **Option 2**: Use existing Communication Service

### Cosmos DB Conflicts

When Cosmos DB has conflicts or region issues:

- Automatically switches **only** Cosmos DB to `eastus2` region
- Keeps other resources in original region
- Uses existing Cosmos DB if available in `eastus2`

### App Service Conflicts

When App Services already exist:

- **Option 1**: Use existing App Services (skip creation)
- **Option 2**: Delete existing App Services and recreate
- **Option 3**: Exit

## Rollback Mechanism

The script tracks all created resources during deployment. If deployment fails:

1. Script detects created resources
2. Prompts user: "Do you want to rollback (delete created resources)?"
3. If yes, deletes all tracked resources
4. Optionally deletes empty resource group

### Manual Rollback

If you need to rollback after script completion, you can use the rollback functions:

```powershell
Import-Module .\scripts\deploy\RollbackHelpers.psm1
Invoke-Rollback
```

## Transient Error Retry

The script automatically retries transient errors with exponential backoff:

- **Transient errors detected**: ServiceUnavailable, TooManyRequests, RequestTimeout, GatewayTimeout, InternalServerError, rate limits, network issues
- **Retry strategy**: Exponential backoff (2s → 4s → 8s → 16s, max 30s)
- **Max retries**: 3 attempts

## Resource Summary

After successful deployment, the script shows a comprehensive resource summary:

- Resource Group and Location
- All resources with their types and states
- **App Services**: URLs, states, locations
- **Storage Accounts**: Primary endpoints
- **Cosmos DB**: Document endpoints, provisioning states
- **Static Web App**: URL, location

## Error Handling

### Error Formatting

All errors are formatted consistently with:

- Step name where error occurred
- Error code (if available)
- User-friendly error message
- Detailed error information

### Error Types

1. **Transient Errors**: Automatically retried with exponential backoff
2. **Conflict Errors**: Handled with user prompts
3. **Validation Errors**: Clear error messages with guidance
4. **Fatal Errors**: Rollback offered if resources were created

## Logging

### Log File Format

``` text
[2024-01-15 10:30:45] [INFO] Starting infrastructure deployment
[2024-01-15 10:30:45] [INFO] Resource Group: dev-euw-rg-mystira-app, Location: westeurope
[2024-01-15 10:31:20] [INFO] Step: Deployment succeeded in 35.2 seconds
[2024-01-15 10:31:20] [ERROR] Error at step: Infrastructure deployment
```

### Log Levels

- **INFO**: Normal operations
- **WARN**: Warnings (non-fatal issues)
- **ERROR**: Errors (deployment failures)

## Best Practices

1. **Use `-WhatIf` first**: Preview deployment before executing
2. **Enable logging**: Use `-LogPath` for troubleshooting
3. **Monitor progress**: Watch for progress indicators during long deployments
4. **Review resource summary**: Check all endpoints after deployment
5. **Keep logs**: Save log files for audit purposes

## Troubleshooting

### Script Hangs

- Use `-SkipScan` to skip resource scanning
- Check network connectivity
- Verify Azure CLI is up to date: `az upgrade`

### Deployment Fails

- Check log file (if `-LogPath` was used)
- Review error message for specific issue
- Use `-Verbose` for detailed output
- Check Azure portal for resource state

### Resource Conflicts

- Script will prompt for resolution
- Choose "use existing" to avoid recreation
- Choose "delete" to start fresh (be careful!)

### Rollback Issues

- Some resources may not delete immediately (Azure propagation delay)
- Check Azure portal for remaining resources
- Manually delete if needed

## Examples

### Example 1: First-Time Deployment

```powershell
# Deploy infrastructure to South Africa North
.\.deploy-now.ps1 -Region "southafricanorth" -LogPath ".\deploy.log"
```

### Example 2: Code Deployment (Infrastructure Exists)

```powershell
# Push code to trigger CI/CD
.\.deploy-now.ps1 -Region "southafricanorth" -Branch "dev" -Message "Deploy latest changes"
```

### Example 3: Preview Deployment

```powershell
# See what would be deployed without actually deploying
.\.deploy-now.ps1 -Region "westeurope" -WhatIf -Verbose
```

### Example 4: Troubleshooting

```powershell
# Full verbose output with logging
.\.deploy-now.ps1 -Region "southafricanorth" -LogPath ".\debug.log" -Verbose
```

## Constants

The script uses the following constants (defined at the top):

- `$MAX_DEPLOYMENT_RETRIES = 3`: Maximum retry attempts for deployment
- `$AZURE_CLI_TIMEOUT_SECONDS = 30`: Timeout for Azure CLI commands
- `$DEPLOYMENT_POLL_INTERVAL_SECONDS = 10`: Polling interval for deployment status

## Status

### ✅ All Issues Resolved (22/22)

- **Critical Issues (1-5)**: ✅ Complete
- **Medium Priority (6-10)**: ✅ Complete
- **Code Quality (19-22)**: ✅ Complete
- **Improvement Opportunities (11-18)**: ✅ Complete (2 deferred as complex features)

### Features Implemented

- ✅ Smart deployment detection
- ✅ Conflict resolution for all resource types
- ✅ Rollback mechanism
- ✅ Transient error retry
- ✅ Logging system
- ✅ Progress indicators
- ✅ Resource summaries
- ✅ Error formatting
- ✅ Input validation
- ✅ Git validation
- ✅ Region validation
- ✅ Dry-run mode

## Related Files

- `.deploy-now.ps1` - Main deployment script
- `scripts/deploy/` - Helper modules
- `infrastructure/dev/main.bicep` - Bicep template
- `.github/workflows/` - CI/CD workflows

## Support

For issues or questions:

1. Check the log file (if `-LogPath` was used)
2. Run with `-Verbose` for detailed output
3. Review error messages for specific guidance
4. Check Azure portal for resource state

---

**Last Updated**: 2024-01-15  
**Version**: 2.0  
**Status**: Production Ready ✅
