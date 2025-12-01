# Deploy Now Script - Smart deployment
# Checks if Azure resources exist:
#   - If NO → Deploy infrastructure
#   - If YES → Push code to trigger CI/CD
# Usage: .\.deploy-now.ps1 [region] [branch] [message] [-SkipScan] [-WhatIf] [-SubscriptionId] [-Verbose]

param(
    [string]$Region = "",
    [string]$Branch = "",
    [string]$Message = "Trigger deployment",
    [switch]$Verbose,
    [switch]$SkipScan,
    [switch]$WhatIf,
    [string]$SubscriptionId = "22f9eb18-6553-4b7d-9451-47d0195085fe",
    [string]$LogPath = ""
)

# Logging function
$script:LogFile = $null
if ($LogPath) {
    $script:LogFile = $LogPath
    if (-not (Test-Path (Split-Path $LogPath -Parent))) {
        New-Item -ItemType Directory -Path (Split-Path $LogPath -Parent) -Force | Out-Null
    }
    "=== Deployment Log Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===" | Out-File -FilePath $LogPath -Append
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logMessage = "[$timestamp] [$Level] $Message"
    if ($script:LogFile) {
        $logMessage | Out-File -FilePath $script:LogFile -Append
    }
    if ($Level -eq "ERROR") {
        Write-ColorOutput Red $logMessage
    }
    elseif ($Level -eq "WARN") {
        Write-ColorOutput Yellow $logMessage
    }
    else {
        Write-Output $logMessage
    }
}

# Colors for output
function Write-ColorOutput($ForegroundColor, $Message) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $host.UI.RawUI.ForegroundColor = $fc
    if ($script:LogFile) {
        $Message | Out-File -FilePath $script:LogFile -Append
    }
}

# Import helper modules
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$deployModulePath = Join-Path $scriptDir "scripts\deploy"
if (Test-Path $deployModulePath) {
    Import-Module (Join-Path $deployModulePath "AzureHelpers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "ResourceHelpers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "ErrorHandlers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "ErrorFormatter.psm1") -Force
    Import-Module (Join-Path $deployModulePath "RollbackHelpers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "RetryHelpers.psm1") -Force
}

# Constants
$script:MAX_DEPLOYMENT_RETRIES = 3
$script:AZURE_CLI_TIMEOUT_SECONDS = 30
$script:DEPLOYMENT_POLL_INTERVAL_SECONDS = 10

# Azure config
$SUB = $SubscriptionId
$REGIONS = @("southafricanorth", "eastus2", "westus2", "centralus", "westeurope", "northeurope", "eastasia")

# Validate region if provided
if ($Region) {
    if (-not (Test-Region -Region $Region -SupportedRegions $REGIONS)) {
        Write-ColorOutput Red "❌ Invalid region: $Region"
        Write-Output "Supported regions: $($REGIONS -join ', ')"
        exit 1
    }
    $LOCATION = $Region
}
else {
    $LOCATION = $REGIONS[0]
}

# Functions are now in ResourceHelpers module

Write-ColorOutput Cyan "🚀 Deploy Now - Smart Deployment"
Write-Output "================================="
Write-Output ""

# Check Azure login
Write-Host "Checking Azure login... " -NoNewline
$account = Test-AzureLogin
if (-not $account) {
    Write-Host "✗" -ForegroundColor Red
    Write-Output "Logging in with device code..."
    az login --use-device-code
    $account = Test-AzureLogin
    if (-not $account) {
        Write-ColorOutput Red "❌ Failed to login to Azure"
        exit 1
    }
}
Write-Host "✓" -ForegroundColor Green
Write-Output "  ($($account.name))"

# Set subscription
Write-Host "Setting subscription... " -NoNewline
if (Set-AzureSubscription -SubscriptionId $SUB) {
    Write-Host "✓" -ForegroundColor Green
}
else {
    Write-Host "✗" -ForegroundColor Red
    Write-Output "Available subscriptions:"
    az account list --output table
    exit 1
}

# Functions are now in modules

# Function to display resource summary
function Show-ResourceSummary {
    param(
        [string]$ResourceGroup,
        [string]$Location
    )
    
    Write-Output ""
    Write-ColorOutput Cyan "================================================"
    Write-ColorOutput Cyan "  📊 Resource Summary"
    Write-ColorOutput Cyan "================================================"
    Write-Output ""
    
    # Get all resources in the resource group
    try {
        $resources = az resource list --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
        if ($resources) {
            Write-Output "Resource Group: $ResourceGroup"
            Write-Output "Location: $Location"
            Write-Output ""
            Write-Output "Resources:"
            
            $appServices = @()
            $storageAccounts = @()
            $cosmosAccounts = @()
            $commServices = @()
            
            foreach ($resource in $resources | Sort-Object type, name) {
                $resourceType = $resource.type -replace '.*/', ''
                $resourceName = $resource.name
                $resourceState = if ($resource.properties.provisioningState) { $resource.properties.provisioningState } else { "N/A" }
                
                Write-Output "  • $resourceType : $resourceName ($resourceState)"
                
                # Categorize resources for detailed info
                if ($resourceType -eq "sites") {
                    $appServices += $resource
                }
                elseif ($resourceType -eq "storageAccounts") {
                    $storageAccounts += $resource
                }
                elseif ($resourceType -eq "databaseAccounts") {
                    $cosmosAccounts += $resource
                }
                elseif ($resourceType -eq "communicationServices") {
                    $commServices += $resource
                }
            }
            
            # Show detailed info for App Services
            if ($appServices.Count -gt 0) {
                Write-Output ""
                Write-Output "App Services:"
                foreach ($app in $appServices) {
                    try {
                        $appInfo = az webapp show --name $app.name --resource-group $ResourceGroup --query "{Url:defaultHostName, State:state, Location:location}" -o json 2>$null | ConvertFrom-Json
                        if ($appInfo) {
                            Write-Output "  • $($app.name):"
                            Write-Output "    URL: https://$($appInfo.Url)"
                            Write-Output "    State: $($appInfo.State)"
                            Write-Output "    Location: $($appInfo.Location)"
                        }
                    }
                    catch {
                        Write-Output "  • $($app.name): (details unavailable)"
                    }
                }
            }
            
            # Show detailed info for Storage Accounts
            if ($storageAccounts.Count -gt 0) {
                Write-Output ""
                Write-Output "Storage Accounts:"
                foreach ($storage in $storageAccounts) {
                    Write-Output "  • $($storage.name)"
                    Write-Output "    Primary Endpoint: https://$($storage.name).blob.core.windows.net"
                }
            }
            
            # Show detailed info for Cosmos DB
            if ($cosmosAccounts.Count -gt 0) {
                Write-Output ""
                Write-Output "Cosmos DB Accounts:"
                foreach ($cosmos in $cosmosAccounts) {
                    try {
                        $cosmosInfo = az cosmosdb show --name $cosmos.name --resource-group $ResourceGroup --query "{DocumentEndpoint:documentEndpoint, ProvisioningState:provisioningState}" -o json 2>$null | ConvertFrom-Json
                        if ($cosmosInfo) {
                            Write-Output "  • $($cosmos.name):"
                            Write-Output "    Endpoint: $($cosmosInfo.DocumentEndpoint)"
                            Write-Output "    State: $($cosmosInfo.ProvisioningState)"
                        }
                    }
                    catch {
                        Write-Output "  • $($cosmos.name): (details unavailable)"
                    }
                }
            }
        }
        
        # Check for Static Web App
        $swaName = Get-StaticWebAppName $Location
        try {
            $swaInfo = az staticwebapp show --name $swaName --resource-group $ResourceGroup 2>$null | ConvertFrom-Json
            if ($swaInfo) {
                Write-Output ""
                Write-Output "Static Web App:"
                Write-Output "  • Name: $($swaInfo.name)"
                Write-Output "  • URL: https://$($swaInfo.defaultHostname)"
                Write-Output "  • Location: $($swaInfo.location)"
            }
        }
        catch {
            Write-Output ""
            Write-ColorOutput Yellow "  ⚠️  Static Web App not found: $swaName"
        }
    }
    catch {
        Write-ColorOutput Yellow "  Could not retrieve resource details"
    }
    
    Write-Output ""
    Write-ColorOutput Cyan "================================================"
    Write-Output ""
}

# Scan for existing resources
$existingResources = Get-ExistingResources -SkipScan:$SkipScan -Verbose:$Verbose -TimeoutSeconds $script:AZURE_CLI_TIMEOUT_SECONDS

$RG = Get-ResourceGroupName $LOCATION
$SWA_NAME = Get-StaticWebAppName $LOCATION

# Check if we have existing resources
$rgExists = $false
$hasResources = $false
$swaExists = $false

# Check if the default resource group exists
$matchingRG = $existingResources.ResourceGroups | Where-Object { $_.Name -eq $RG }
if ($matchingRG) {
    $rgExists = $true
    $hasResources = $matchingRG.HasResources
}

# Check if Static Web App exists
$matchingSWA = $existingResources.StaticWebApps | Where-Object { $_.Name -eq $SWA_NAME -and $_.ResourceGroup -eq $RG }
if ($matchingSWA) {
    $swaExists = $true
}

# If we have existing resources, show them and ask what to do
if ($existingResources.ResourceGroups.Count -gt 0 -or $existingResources.StaticWebApps.Count -gt 0) {
    Write-Output ""
    Write-ColorOutput Cyan "📋 Found Existing Resources:"
    Write-Output ""
    
    if ($existingResources.ResourceGroups.Count -gt 0) {
        Write-Output "Resource Groups:"
        for ($i = 0; $i -lt $existingResources.ResourceGroups.Count; $i++) {
            $rgItem = $existingResources.ResourceGroups[$i]
            $status = if ($rgItem.HasResources) { "✓ Has resources ($($rgItem.ResourceCount))" } else { "Empty" }
            Write-Output "  [$($i+1)] $($rgItem.Name) - $($rgItem.Location) ($status)"
        }
        Write-Output ""
    }
    
    if ($existingResources.StaticWebApps.Count -gt 0) {
        Write-Output "Static Web Apps:"
        for ($i = 0; $i -lt $existingResources.StaticWebApps.Count; $i++) {
            $swaItem = $existingResources.StaticWebApps[$i]
            Write-Output "  [$($i+1)] $($swaItem.Name) - $($swaItem.ResourceGroup) ($($swaItem.Location))"
            Write-Output "      URL: https://$($swaItem.DefaultHostname)"
        }
        Write-Output ""
    }
    
    Write-ColorOutput Yellow "What would you like to do?"
    $maxOption = 4
    $options = @("1. Use existing resource group: $RG (in $LOCATION)")
    if ($existingResources.ResourceGroups.Count -gt 0) {
        $options += "2. Use a different existing resource group"
        $maxOption = 4
    }
    else {
        $maxOption = 3
    }
    $options += "3. Create new resource group with new name"
    $options += "4. Create new resource group in different region"
    
    foreach ($opt in $options) {
        Write-Output "  $opt"
    }
    Write-Output ""
    
    # Input validation with retry
    do {
        $choice = Read-Host "Choose an option (1-$maxOption)"
        if ($choice -notmatch "^[1-$maxOption]$") {
            Write-ColorOutput Red "❌ Invalid choice. Please enter a number between 1 and $maxOption."
        }
    } while ($choice -notmatch "^[1-$maxOption]$")
    
    switch ($choice) {
        "1" {
            # Use default - continue as normal
            Write-ColorOutput Green "✅ Using existing resource group: $RG"
        }
        "2" {
            if ($existingResources.ResourceGroups.Count -gt 0) {
                Write-Output ""
                Write-Output "Select a resource group:"
                for ($i = 0; $i -lt $existingResources.ResourceGroups.Count; $i++) {
                    $rgItem = $existingResources.ResourceGroups[$i]
                    Write-Output "  [$($i+1)] $($rgItem.Name) - $($rgItem.Location)"
                }
                Write-Output ""
                # Input validation with retry
                do {
                    $rgChoice = Read-Host "Enter number (1-$($existingResources.ResourceGroups.Count))"
                    if ($rgChoice -notmatch "^\d+$" -or [int]$rgChoice -lt 1 -or [int]$rgChoice -gt $existingResources.ResourceGroups.Count) {
                        Write-ColorOutput Red "❌ Invalid choice. Please enter a number between 1 and $($existingResources.ResourceGroups.Count)."
                    }
                } while ($rgChoice -notmatch "^\d+$" -or [int]$rgChoice -lt 1 -or [int]$rgChoice -gt $existingResources.ResourceGroups.Count)
                
                $selectedResourceGroup = $existingResources.ResourceGroups[[int]$rgChoice - 1]
                $RG = $selectedResourceGroup.Name
                $LOCATION = $selectedResourceGroup.Location
                $SWA_NAME = Get-StaticWebAppName $LOCATION
                Write-ColorOutput Green "✅ Selected: $RG in $LOCATION"
            }
        }
        "3" {
            $newName = Read-Host "Enter new resource group name (e.g., dev-custom-rg-mystira-app)"
            $RG = $newName
            $rgExists = $false
            $hasResources = $false
            Write-ColorOutput Green "✅ Will create new resource group: $RG"
        }
        "4" {
            Write-Output ""
            Write-Output "Available regions:"
            for ($i = 0; $i -lt $REGIONS.Count; $i++) {
                Write-Output "  [$($i+1)] $($REGIONS[$i])"
            }
            Write-Output ""
            # Input validation with retry
            do {
                $regionChoice = Read-Host "Enter region number (1-$($REGIONS.Count))"
                if ($regionChoice -notmatch "^\d+$" -or [int]$regionChoice -lt 1 -or [int]$regionChoice -gt $REGIONS.Count) {
                    Write-ColorOutput Red "❌ Invalid choice. Please enter a number between 1 and $($REGIONS.Count)."
                }
            } while ($regionChoice -notmatch "^\d+$" -or [int]$regionChoice -lt 1 -or [int]$regionChoice -gt $REGIONS.Count)
                        
            $selectedRegion = $REGIONS[[int]$regionChoice - 1]
            if (-not (Test-Region -Region $selectedRegion -SupportedRegions $REGIONS)) {
                Write-ColorOutput Red "❌ Invalid region selected"
                exit 1
            }
            $LOCATION = $selectedRegion
            $RG = Get-ResourceGroupName $LOCATION
            $SWA_NAME = Get-StaticWebAppName $LOCATION
            $rgExists = $false
            $hasResources = $false
            Write-ColorOutput Green "✅ Will create in region: $LOCATION (Resource Group: $RG)"
        }
        default {
            Write-ColorOutput Red "❌ Invalid choice. Using default: $RG"
        }
    }
    
    Write-Output ""
    
    # Re-check Static Web App existence after selection
    $swaExists = $false
    try {
        $swaInfo = az staticwebapp show --name $SWA_NAME --resource-group $RG 2>$null | ConvertFrom-Json
        if ($swaInfo) {
            $swaExists = $true
        }
    }
    catch {
        # Static Web App doesn't exist
    }
    
    # Re-check resource group existence
    $rgExists = $false
    $hasResources = $false
    try {
        $rgInfo = az group show --name $RG 2>$null | ConvertFrom-Json
        if ($rgInfo) {
            $rgExists = $true
            $resources = az resource list --resource-group $RG --output json 2>$null | ConvertFrom-Json
            if ($resources -and $resources.Count -gt 0) {
                $hasResources = $true
            }
        }
    }
    catch {
        # Resource group doesn't exist
    }
}

if ($hasResources -and $swaExists) {
    Write-ColorOutput Green "✅ Infrastructure is deployed. Pushing code to trigger CI/CD..."
    Write-Output ""
    
    # Code deployment path
    Write-Log "Step: Validating git repository" "INFO"
    
    # Validate git repo
    try {
        $gitRoot = git rev-parse --git-dir 2>$null
        if (-not $gitRoot) {
            Write-ColorOutput Red "❌ Not in a git repository. Please run this script from the repository root."
            Write-Log "Error: Not in a git repository" "ERROR"
            exit 1
        }
        Write-Log "Git repository validated: $gitRoot" "INFO"
    }
    catch {
        Write-ColorOutput Red "❌ Git not available or not in a git repository."
        Write-Log "Error: Git validation failed" "ERROR"
        exit 1
    }
    
    $CURRENT_BRANCH = git rev-parse --abbrev-ref HEAD
    $TARGET_BRANCH = if ($Branch) { $Branch } else { $CURRENT_BRANCH }
    Write-Log "Current branch: $CURRENT_BRANCH, Target branch: $TARGET_BRANCH" "INFO"
    
    # Check if there are uncommitted changes
    Write-Log "Step: Checking for uncommitted changes" "INFO"
    $status = git status --porcelain
    if ($status) {
        Write-ColorOutput Yellow "⚠️  You have uncommitted changes."
        $response = Read-Host "Do you want to commit them? (y/n)"
        if ($response -eq 'y' -or $response -eq 'Y') {
            git add .
            git commit -m $Message
        }
        else {
            Write-ColorOutput Red "❌ Aborting. Please commit or stash your changes first."
            exit 1
        }
    }
    
    # Check if we're on the target branch
    if ($CURRENT_BRANCH -ne $TARGET_BRANCH) {
        Write-ColorOutput Yellow "⚠️  You're on branch '$CURRENT_BRANCH', but targeting '$TARGET_BRANCH'"
        $response = Read-Host "Do you want to switch to '$TARGET_BRANCH'? (y/n)"
        if ($response -eq 'y' -or $response -eq 'Y') {
            git checkout $TARGET_BRANCH
        }
        else {
            Write-ColorOutput Red "❌ Aborting."
            exit 1
        }
    }
    
    # Fetch latest changes
    Write-ColorOutput Yellow "📥 Fetching latest changes..."
    git fetch origin
    
    # Check if we're behind remote
    $behind = git rev-list HEAD..origin/$TARGET_BRANCH --count 2>$null
    if ($behind -gt 0) {
        Write-ColorOutput Yellow "⚠️  Your branch is behind origin/$TARGET_BRANCH"
        $response = Read-Host "Do you want to pull latest changes? (y/n)"
        if ($response -eq 'y' -or $response -eq 'Y') {
            git pull origin $TARGET_BRANCH
        }
    }
    
    # Check if we're ahead of remote
    $LOCAL_COMMITS = git rev-list origin/$TARGET_BRANCH..HEAD --count 2>$null
    if ($LOCAL_COMMITS -eq 0) {
        # Create an empty commit to trigger deployment
        Write-ColorOutput Yellow "📝 Creating empty commit to trigger deployment..."
        git commit --allow-empty -m $Message
    }
    
    # Push to remote
    Write-ColorOutput Yellow "📤 Pushing to origin/$TARGET_BRANCH..."
    git push origin $TARGET_BRANCH
    
    Write-Output ""
    Write-ColorOutput Green "✅ Code deployment triggered!"
    Write-ColorOutput Green "   Branch: $TARGET_BRANCH"
    Write-ColorOutput Green "   Check GitHub Actions for deployment status"
    Write-Output ""
    
    # Show resource summary
    Show-ResourceSummary -ResourceGroup $RG -Location $LOCATION
}
else {
    Write-ColorOutput Yellow "No resources found"
    Write-Output ""
    Write-ColorOutput Cyan "🏗️  Infrastructure not deployed. Deploying infrastructure..."
    Write-Output ""
    Write-Output "Region: $LOCATION"
    Write-Output "Resource Group: $RG"
    Write-Output ""
    
    # Create resource group if it doesn't exist
    if (-not $rgExists) {
        Write-Host "Creating resource group... " -NoNewline
        az group create --name $RG --location $LOCATION --output none 2>$null
        Write-Host "✓" -ForegroundColor Green
    }
    
    # Find bicep template
    $SCRIPT_DIR = $PSScriptRoot
    $BICEP_FILE = $null
    
    $paths = @(
        "$SCRIPT_DIR\infrastructure\dev\main.bicep",
        "$SCRIPT_DIR\src\Mystira.App.Infrastructure.Azure\Deployment\dev\main.bicep",
        ".\infrastructure\dev\main.bicep",
        ".\src\Mystira.App.Infrastructure.Azure\Deployment\dev\main.bicep"
    )
    
    foreach ($path in $paths) {
        if (Test-Path $path) {
            $BICEP_FILE = $path
            break
        }
    }
    
    if (-not $BICEP_FILE) {
        Write-ColorOutput Red "❌ Can't find main.bicep"
        Write-Output "Run this from the repo root."
        exit 1
    }
    
    Write-Output "Using template: $BICEP_FILE"
    Write-Output ""
    
    # Generate JWT secret
    $JWT_SECRET = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | ForEach-Object { [char]$_ })
    $JWT_SECRET_BASE64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($JWT_SECRET))
    
    # Get resource prefix based on location (matches infrastructure/dev/main.bicep)
    $resourcePrefix = switch ($LOCATION) {
        "southafricanorth" { "dev-san" }
        "eastus2" { "dev-eus2" }
        "westus2" { "dev-wus2" }
        "centralus" { "dev-cus" }
        "westeurope" { "dev-euw" }
        "northeurope" { "dev-eun" }
        "eastasia" { "dev-ea" }
        default { "dev-$($LOCATION.Substring(0, 4))" }
    }
    
    # Calculate expected storage name (needed for template)
    $storageNameBase = ($resourcePrefix + "-st-mystira") -replace '-', ''
    if ($storageNameBase.Length -gt 24) {
        $expectedStorageName = $storageNameBase.Substring(0, 24)
    }
    else {
        $expectedStorageName = $storageNameBase
    }
    
    Write-ColorOutput Cyan "🚀 Deploying infrastructure..."
    Write-Output "   (Will handle conflicts if they occur)"
    Write-Output ""
    
    # Initialize rollback tracking
    $deployName = "mystira-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Initialize-RollbackTracking -ResourceGroup $RG -DeploymentName $deployName
    
    # Deploy infrastructure - try first, handle conflicts on failure
    $retryCount = 0
    $script:cosmosUseAttempted = $false
    $script:skipCosmosCreation = $false
    $script:skipStorageCreation = $false
    $script:skipCommServiceCreation = $false
    $script:skipAppServiceCreation = $false
    $script:appServiceUseAttempted = $false
    if (-not $script:existingAppServiceResourceGroup) { $script:existingAppServiceResourceGroup = "" }
    if (-not $script:existingCosmosResourceGroup) { $script:existingCosmosResourceGroup = "" }
    if (-not $script:existingCosmosDbAccountName) { $script:existingCosmosDbAccountName = "" }
    
    while ($retryCount -le $script:MAX_DEPLOYMENT_RETRIES) {
        # Ensure resource group exists in current region
        $rgCheck = az group show --name $RG 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
        if (-not $rgCheck) {
            Write-Host "Creating resource group $RG in $LOCATION..." -NoNewline
            az group create --name $RG --location $LOCATION --output none 2>$null
            Write-Host " ✓" -ForegroundColor Green
        }
        
        # Create parameters JSON file for secure parameters
        $paramsFile = Join-Path $env:TEMP "bicep-params-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
        
        # Build parameters object - preserve skip flags across retries using script-scoped variables
        if ($Verbose -and $retryCount -gt 0) {
            $skipAppService = $script:skipAppServiceCreation
            $existingAppServiceRG = $script:existingAppServiceResourceGroup
            $verboseMsg = "   [VERBOSE] Retry $retryCount : skipAppServiceCreation=$skipAppService, existingAppServiceResourceGroup=$existingAppServiceRG"
            Write-ColorOutput Yellow $verboseMsg
        }
        
        $paramsObj = @{
            environment             = @{ value = "dev" }
            location                = @{ value = $LOCATION }
            resourcePrefix          = @{ value = $resourcePrefix }
            jwtSecretKey            = @{ value = $JWT_SECRET_BASE64 }
            newStorageAccountName   = @{ value = $expectedStorageName }
            skipCosmosCreation      = @{ value = [bool]$script:skipCosmosCreation }
            skipStorageCreation     = @{ value = [bool]$script:skipStorageCreation }
            skipCommServiceCreation = @{ value = [bool]$script:skipCommServiceCreation }
            skipAppServiceCreation  = @{ value = [bool]$script:skipAppServiceCreation }
        }
        
        # Add existing resource parameters - use script-scoped variables (they persist across retries)
        if (-not $script:existingAppServiceResourceGroup) { $script:existingAppServiceResourceGroup = "" }
        if (-not $script:existingCosmosResourceGroup) { $script:existingCosmosResourceGroup = "" }
        if (-not $script:existingCosmosDbAccountName) { $script:existingCosmosDbAccountName = "" }
        
        $paramsObj.existingAppServiceResourceGroup = @{ value = $script:existingAppServiceResourceGroup }
        $paramsObj.existingCosmosResourceGroup = @{ value = $script:existingCosmosResourceGroup }
        $paramsObj.existingCosmosDbAccountName = @{ value = $script:existingCosmosDbAccountName }
        
        $paramsContent = @{}
        $paramsContent.'$schema' = "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#"
        $paramsContent.contentVersion = "1.0.0.0"
        $paramsContent.parameters = $paramsObj
        
        # Write JSON with proper formatting
        $jsonContent = $paramsContent | ConvertTo-Json -Depth 10 -Compress:$false
        [System.IO.File]::WriteAllText($paramsFile, $jsonContent, [System.Text.Encoding]::UTF8)
        
        Write-Output "Using parameters file: $paramsFile"
        Write-Output ""
        
        if ($WhatIf) {
            Write-ColorOutput Cyan "🔍 [WHATIF] Would deploy infrastructure with the following parameters:"
            $paramsDebug = Get-Content $paramsFile | ConvertFrom-Json
            Write-Output ($paramsDebug.parameters | ConvertTo-Json -Depth 10)
            Write-Output ""
            Write-ColorOutput Yellow "   [WHATIF] Deployment would target:"
            Write-Output "   Resource Group: $RG"
            Write-Output "   Location: $LOCATION"
            Write-Output "   Template: $BICEP_FILE"
            Write-Output ""
            Write-ColorOutput Green "✅ [WHATIF] Preview complete. Run without -WhatIf to deploy."
            $deploymentSuccess = $true
            break
        }
        
        Write-ColorOutput Cyan "🚀 Starting infrastructure deployment..."
        Write-Output "This may take several minutes. Please wait..."
        Write-Output ""
        Write-Log "Starting infrastructure deployment" "INFO"
        Write-Log "Resource Group: $RG, Location: $LOCATION, Template: $BICEP_FILE" "INFO"
        
        $deploymentSuccess = $false
        $shouldRetry = $false
        $deploymentStartTime = Get-Date
        
        try {
            Write-Log "Step: Executing Bicep deployment" "INFO"
            if ($Verbose) {
                Write-ColorOutput Yellow "   [VERBOSE] Parameters file contents:"
                $paramsDebug = Get-Content $paramsFile | ConvertFrom-Json
                Write-Output ($paramsDebug.parameters | ConvertTo-Json -Depth 10)
                Write-Output ""
            }
            
            # Show progress indicator
            Write-Host "Deploying..." -NoNewline
            $progressJob = Start-Job -ScriptBlock {
                $dots = 0
                while ($true) {
                    Start-Sleep -Seconds 2
                    $dots = ($dots + 1) % 4
                    $progress = "." * $dots + " " * (3 - $dots)
                    Write-Host "`rDeploying$progress" -NoNewline
                }
            }
            
            # Capture both stdout and stderr separately with retry logic for transient errors
            $deploymentResult = Invoke-WithRetry -ScriptBlock {
                $output = az deployment group create `
                    --resource-group $RG `
                    --template-file $BICEP_FILE `
                    --parameters "@$paramsFile" `
                    --mode Incremental `
                    --name $deployName `
                    --output json 2>&1 | Where-Object { $_ -notmatch '^WARNING:' }
                
                if ($LASTEXITCODE -ne 0) {
                    throw ($output -join "`n")
                }
                
                return $output
            } -MaxRetries 3 -InitialDelaySeconds 5 -MaxDelaySeconds 30
            
            if (-not $deploymentResult.Success) {
                # Check if it's a transient error that we should retry
                if ($deploymentResult.IsTransient) {
                    Write-ColorOutput Yellow "⚠️  Transient error detected, will retry with conflict handling..."
                    $stdout = $deploymentResult.Error
                }
                else {
                    $stdout = $deploymentResult.Error
                }
                $LASTEXITCODE = 1
            }
            else {
                $stdout = $deploymentResult.Result
                $LASTEXITCODE = 0
            }
            
            # Stop progress indicator
            $progressJob | Stop-Job -ErrorAction SilentlyContinue
            $progressJob | Remove-Job -ErrorAction SilentlyContinue
            Write-Host "`r" -NoNewline  # Clear progress line
            
            if ($LASTEXITCODE -eq 0) {
                # Success - try to parse JSON
                $elapsed = (Get-Date) - $deploymentStartTime
                Write-Host "✓ Deployment completed in $([math]::Round($elapsed.TotalMinutes, 1)) minutes" -ForegroundColor Green
                Write-Log "Step: Deployment succeeded in $([math]::Round($elapsed.TotalSeconds, 2)) seconds" "INFO"
                try {
                    $result = $stdout | ConvertFrom-Json -ErrorAction Stop
                    if (-not $Verbose) {
                        Write-Output "Deployment successful!"
                    }
                    else {
                        Write-Output ($result | ConvertTo-Json -Depth 10 | Out-String)
                    }
                    
                    # Track created resources from deployment output
                    if ($result.properties.outputs) {
                        if ($result.properties.outputs.storageAccountName) {
                            Register-Resource -ResourceType "Microsoft.Storage/storageAccounts" `
                                -ResourceName $result.properties.outputs.storageAccountName.value `
                                -ResourceGroup $RG
                        }
                        if ($result.properties.outputs.cosmosDbAccountName) {
                            Register-Resource -ResourceType "Microsoft.DocumentDB/databaseAccounts" `
                                -ResourceName $result.properties.outputs.cosmosDbAccountName.value `
                                -ResourceGroup $RG
                        }
                        if ($result.properties.outputs.communicationServiceName) {
                            Register-Resource -ResourceType "Microsoft.Communication/communicationServices" `
                                -ResourceName $result.properties.outputs.communicationServiceName.value `
                                -ResourceGroup $RG
                        }
                    }
                    
                    $deploymentSuccess = $true
                }
                catch {
                    # If JSON parse fails but exit code is 0, deployment might have succeeded
                    Write-Output "Deployment completed (output parsing skipped)"
                    $deploymentSuccess = $true
                }
            }
            else {
                # Failure - try to parse error JSON
                $elapsed = (Get-Date) - $deploymentStartTime
                Write-Log "Step: Deployment failed after $([math]::Round($elapsed.TotalSeconds, 2)) seconds" "ERROR"
                $errorJson = $null
                $errorMsg = ""
                
                try {
                    $errorJson = $stdout | ConvertFrom-Json -ErrorAction Stop
                    if ($errorJson.error) {
                        $errorMsg = $errorJson.error.message
                        Write-Log "Error details: $errorMsg" "ERROR"
                    }
                }
                catch {
                    # Not JSON, use raw output
                    $errorMsg = ($stdout -join "`n")
                    Write-Log "Error (non-JSON): $errorMsg" "ERROR"
                }
                
                if (-not $errorMsg) {
                    $errorMsg = ($stdout -join "`n")
                    Write-Log "Error (raw): $errorMsg" "ERROR"
                }
                
                # If we already tried "use" for Communication Service and it failed again, don't retry
                if ($script:commServiceUseAttempted -and $errorMsg -match "NameReservationTaken" -and $errorMsg -match "communication") {
                    Write-Output ""
                    $formattedError = Format-Error -ErrorMessage $errorMsg -Step "Communication Service conflict resolution"
                    Write-ColorOutput Red $formattedError
                    Write-Log "Communication Service 'use' failed after retry" "ERROR"
                    exit 1
                }
                
                # Check for App Service errors FIRST (before handling other conflicts)
                # If we already tried using existing App Services in a PREVIOUS retry and it failed again, don't retry
                if ($script:appServiceUseAttempted -and ($errorMsg -match "Website.*already exists" -or ($errorMsg -match "Conflict" -and $errorMsg -match "Website"))) {
                    Write-Output ""
                    $formattedError = Format-Error -ErrorMessage $errorMsg -Step "App Service conflict resolution"
                    Write-ColorOutput Red $formattedError
                    Write-ColorOutput Yellow "   The App Services exist but the deployment is still trying to create them."
                    Write-ColorOutput Cyan "   This may indicate the skipAppServiceCreation parameter isn't being applied correctly."
                    Write-ColorOutput Cyan "   Parameter should be: skipAppServiceCreation=true, existingAppServiceResourceGroup=$RG"
                    Write-Log "App Service 'use existing' failed after retry" "ERROR"
                    exit 1
                }
                
                # Handle App Service conflicts (they already exist - provide detailed info and options)
                if ($errorMsg -match "Website.*already exists" -or ($errorMsg -match "Conflict" -and $errorMsg -match "Website")) {
                    Handle-AppServiceConflict `
                        -ErrorMsg $errorMsg `
                        -ResourcePrefix $resourcePrefix `
                        -RG $RG `
                        -Location ([ref]$LOCATION) `
                        -ResourceGroup ([ref]$RG) `
                        -ResourcePrefixRef ([ref]$resourcePrefix) `
                        -ParamsObj ([ref]$paramsObj) `
                        -ParamsContent ([ref]$paramsContent) `
                        -ParamsFile $paramsFile `
                        -ShouldRetry ([ref]$shouldRetry) `
                        -RetryCount ([ref]$retryCount) `
                        -SkipAppServiceCreation ([ref]$script:skipAppServiceCreation) `
                        -ExistingAppServiceResourceGroup ([ref]$script:existingAppServiceResourceGroup) `
                        -AppServiceUseAttempted ([ref]$script:appServiceUseAttempted) `
                        -Verbose:$Verbose `
                        -WhatIf:$WhatIf
                }
                
                # If we already tried using existing Cosmos DB and it failed again with a Cosmos DB error, don't retry
                # BUT only if it's actually a Cosmos DB error (not App Service or other errors)
                # Also check that we haven't just set appServiceUseAttempted (to avoid exiting when App Service retry is needed)
                if ($script:cosmosUseAttempted -and -not $script:appServiceUseAttempted -and ($errorMsg -match "cosmos" -or $errorMsg -match "Cosmos" -or $errorMsg -match "DocumentDB") -and -not ($errorMsg -match "Website" -or $errorMsg -match "App Service" -or $errorMsg -match "appservice" -or $errorMsg -match "Conflict.*Website")) {
                    Write-Output ""
                    $formattedError = Format-Error -ErrorMessage $errorMsg -Step "Cosmos DB conflict resolution"
                    Write-ColorOutput Red $formattedError
                    Write-Log "Cosmos DB 'use existing' failed after retry" "ERROR"
                    exit 1
                }
                
                # Handle storage account conflict - auto switch to eastus2
                if ($errorMsg -match "StorageAccountAlreadyTaken" -or $errorMsg -match "storage account.*already taken") {
                    Handle-StorageConflict `
                        -Location ([ref]$LOCATION) `
                        -ResourceGroup ([ref]$RG) `
                        -ResourcePrefix ([ref]$resourcePrefix) `
                        -ExpectedStorageName ([ref]$expectedStorageName) `
                        -ParamsObj ([ref]$paramsObj) `
                        -ParamsContent ([ref]$paramsContent) `
                        -ParamsFile $paramsFile `
                        -ShouldRetry ([ref]$shouldRetry) `
                        -RetryCount ([ref]$retryCount) `
                        -WhatIf:$WhatIf
                }
                # Handle Communication Services conflict - prompt user
                elseif ($errorMsg -match "NameReservationTaken" -and $errorMsg -match "communication") {
                    Handle-CommunicationServiceConflict `
                        -ResourcePrefix $resourcePrefix `
                        -RG $RG `
                        -Location ([ref]$LOCATION) `
                        -ResourceGroup ([ref]$RG) `
                        -ResourcePrefixRef ([ref]$resourcePrefix) `
                        -ExpectedStorageName ([ref]$expectedStorageName) `
                        -ParamsObj ([ref]$paramsObj) `
                        -ParamsContent ([ref]$paramsContent) `
                        -ParamsFile $paramsFile `
                        -ShouldRetry ([ref]$shouldRetry) `
                        -RetryCount ([ref]$retryCount) `
                        -CommServiceUseAttempted ([ref]$script:commServiceUseAttempted) `
                        -Verbose:$Verbose `
                        -WhatIf:$WhatIf
                }
                # Handle Cosmos DB conflict or region issues - switch ONLY Cosmos DB to eastus2
                elseif (($errorMsg -match "Dns record.*already taken" -or ($errorMsg -match "BadRequest" -and $errorMsg -match "cosmos") -or ($errorMsg -match "ServiceUnavailable" -and $errorMsg -match "cosmos") -or ($errorMsg -match "high demand" -and $errorMsg -match "region") -or ($errorMsg -match "failed provisioning state")) -and -not $script:cosmosUseAttempted) {
                    Handle-CosmosDbConflict `
                        -ErrorMsg $errorMsg `
                        -ResourcePrefix $resourcePrefix `
                        -RG $RG `
                        -ParamsObj ([ref]$paramsObj) `
                        -ParamsContent ([ref]$paramsContent) `
                        -ParamsFile $paramsFile `
                        -ShouldRetry ([ref]$shouldRetry) `
                        -RetryCount ([ref]$retryCount) `
                        -SkipCosmosCreation ([ref]$script:skipCosmosCreation) `
                        -CosmosUseAttempted ([ref]$script:cosmosUseAttempted) `
                        -Verbose:$Verbose `
                        -WhatIf:$WhatIf
                }
                # If we already tried using existing Cosmos DB and it failed again, don't retry
                # BUT only if it's actually a Cosmos DB error (not App Service or other errors)
                # Also check that we haven't just set appServiceUseAttempted (to avoid exiting when App Service retry is needed)
                elseif ($script:cosmosUseAttempted -and -not $script:appServiceUseAttempted -and ($errorMsg -match "cosmos" -or $errorMsg -match "Cosmos" -or $errorMsg -match "DocumentDB") -and -not ($errorMsg -match "Website" -or $errorMsg -match "App Service" -or $errorMsg -match "appservice" -or $errorMsg -match "Conflict.*Website")) {
                    Write-Output ""
                    Write-ColorOutput Red "❌ Cosmos DB 'use existing' failed. Cannot proceed."
                    Write-Output "Error: $errorMsg"
                    exit 1
                }
                else {
                    # Other error - don't retry (unless we've already set shouldRetry for a handled conflict)
                    if (-not $shouldRetry) {
                        Write-Output ""
                        Write-ColorOutput Red "❌ Deployment failed at step: Infrastructure deployment"
                        Write-Log "Deployment failed at step: Infrastructure deployment" "ERROR"
                        Write-Output ""
                        Write-Output "Error: $errorMsg"
                        Write-Log "Error message: $errorMsg" "ERROR"
                        exit 1
                    }
                    # If shouldRetry is true, we've already handled the error above, just continue to retry
                }
            }
        }
        catch {
            $formattedError = Format-Error -ErrorMessage $_.Exception.Message -Step "Infrastructure deployment"
            Write-ColorOutput Red $formattedError
            Write-Log "Exception at step: Infrastructure deployment - $($_.Exception.Message)" "ERROR"
            exit 1
        }
        finally {
            if ($script:LogFile) {
                "=== Deployment Log Ended: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===" | Out-File -FilePath $script:LogFile -Append
            }
            # Clean up temp file
            if (Test-Path $paramsFile) {
                Remove-Item $paramsFile -Force
            }
        }
        
        # If deployment succeeded or we shouldn't retry, break the loop
        if ($Verbose) {
            Write-ColorOutput Yellow "   [VERBOSE] After error handling - deploymentSuccess=$deploymentSuccess, shouldRetry=$shouldRetry"
        }
        if ($deploymentSuccess -or -not $shouldRetry) {
            break
        }
        
        # If we've exceeded max retries
        if ($retryCount -gt $script:MAX_DEPLOYMENT_RETRIES) {
            Write-ColorOutput Red "❌ Maximum retry attempts exceeded ($($script:MAX_DEPLOYMENT_RETRIES))."
            exit 1
        }
    }
    
    # Check final deployment status
    if ($deploymentSuccess) {
        Write-Output ""
        Write-ColorOutput Green "================================================"
        Write-ColorOutput Green "  Infrastructure deployment successful!"
        Write-ColorOutput Green "================================================"
        Write-Output ""
        Write-Output "Resources deployed to: $RG"
        Write-Output "Location: $LOCATION"
        Write-Output ""
        Write-ColorOutput Yellow "JWT Secret (save this!):"
        Write-Output $JWT_SECRET_BASE64
        Write-Output ""
        
        # Check if Static Web App needs to be created (re-check to avoid race condition)
        Write-Log "Step: Checking Static Web App existence" "INFO"
        $swaCheck = $false
        try {
            $swaInfo = az staticwebapp show --name $SWA_NAME --resource-group $RG 2>$null | ConvertFrom-Json
            if ($swaInfo) {
                $swaCheck = $true
                Write-Log "Static Web App already exists: $SWA_NAME" "INFO"
            }
        }
        catch {
            # Static Web App doesn't exist
            Write-Log "Static Web App not found: $SWA_NAME" "INFO"
        }
        
        if (-not $swaCheck) {
            Write-Log "Step: Creating Static Web App" "INFO"
            Write-ColorOutput Yellow "📱 Creating Static Web App..."
            Write-Output ""
            
            # Check name availability first
            Write-Log "Step: Checking Static Web App name availability" "INFO"
            $nameAvailable = $true
            try {
                $existingSWA = az staticwebapp list --query "[?name=='$SWA_NAME']" -o json 2>$null | ConvertFrom-Json
                if ($existingSWA -and $existingSWA.Count -gt 0) {
                    $nameAvailable = $false
                    Write-ColorOutput Yellow "⚠️  Static Web App name '$SWA_NAME' is already taken globally."
                    Write-Log "Warning: Static Web App name already exists globally" "WARN"
                }
            }
            catch {
                # Ignore - name might be available
            }
            
            if ($nameAvailable) {
                # Get GitHub repo info
                $repoUrl = git remote get-url origin
                $repoOwner = ""
                $repoName = ""
                
                if ($repoUrl -match "github\.com[:/]([^/]+)/([^/]+?)(?:\.git)?$") {
                    $repoOwner = $matches[1]
                    $repoName = $matches[2] -replace '\.git$', ''
                }
                
                if ($repoOwner -and $repoName) {
                    Write-Output "Repository: $repoOwner/$repoName"
                    Write-Log "GitHub repository: $repoOwner/$repoName" "INFO"
                    Write-Output ""
                    
                    # Create Static Web App with GitHub integration
                    Write-Host "Creating Static Web App... " -NoNewline
                    Write-Log "Creating Static Web App: $SWA_NAME" "INFO"
                    $swaResult = az staticwebapp create `
                        --name $SWA_NAME `
                        --resource-group $RG `
                        --location $LOCATION `
                        --sku Free `
                        --login-with-github `
                        --source "https://github.com/$repoOwner/$repoName" `
                        --branch "dev" `
                        --app-location "./src/Mystira.App.PWA" `
                        --api-location "swa-db-connections" `
                        --output-location "out" `
                        --output json 2>$null | ConvertFrom-Json
                    
                    if ($swaResult) {
                        Write-Host "✓" -ForegroundColor Green
                        Write-ColorOutput Green "✅ Static Web App created!"
                        Write-Log "Static Web App created successfully: $SWA_NAME" "INFO"
                        Write-Output ""
                        Write-ColorOutput Yellow "⚠️  IMPORTANT: Get the deployment token and add it to GitHub Secrets:"
                        Write-Output ""
                        Write-Output "   Secret name: AZURE_STATIC_WEB_APPS_API_TOKEN_DEV_MYSTIRA_APP"
                        Write-Output ""
                        Write-Output "   Get token with:"
                        Write-ColorOutput Cyan "   az staticwebapp secrets list --name $SWA_NAME --resource-group $RG --query properties.apiKey -o tsv"
                        Write-Output ""
                    }
                    else {
                        Write-Host "✗" -ForegroundColor Red
                        Write-ColorOutput Yellow "⚠️  Static Web App creation may have failed. Check Azure portal."
                        Write-Log "Warning: Static Web App creation may have failed" "WARN"
                    }
                }
                else {
                    Write-ColorOutput Yellow "⚠️  Could not detect GitHub repository."
                    Write-Log "Error: Could not detect GitHub repository" "ERROR"
                    Write-Output "   Please create the Static Web App manually or update the script."
                }
            }
            else {
                Write-ColorOutput Yellow "⚠️  Cannot create Static Web App - name is already taken."
                Write-Log "Error: Static Web App name unavailable" "ERROR"
            }
        }
        else {
            if ($Verbose) {
                Write-ColorOutput Green "   [VERBOSE] Static Web App already exists, skipping creation."
            }
        }
        
        # Show final resource summary
        Show-ResourceSummary -ResourceGroup $RG -Location $LOCATION
    }
}
