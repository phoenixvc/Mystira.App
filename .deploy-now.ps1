# Deploy Now Script - Smart deployment
# Checks if Azure resources exist:
#   - If NO → Deploy infrastructure
#   - If YES → Push code to trigger CI/CD
# Usage: .\.deploy-now.ps1 [region] [branch] [message] [-SkipScan] [-WhatIf] [-SubscriptionId] [-Verbose] [-GitHubPat] [-LogPath]

param(
    [string]$Region = "",
    [string]$Branch = "",
    [string]$Message = "Trigger deployment",
    [switch]$Verbose,
    [switch]$SkipScan,
    [switch]$WhatIf,
    [string]$SubscriptionId = "22f9eb18-6553-4b7d-9451-47d0195085fe",
    [string]$LogPath = "",
    [string]$GitHubPat = ""
)

# Colors for output (define in global scope so modules can use it)
function global:Write-ColorOutput($ForegroundColor, $Message) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $host.UI.RawUI.ForegroundColor = $fc
    if ($script:LogFile) {
        $Message | Out-File -FilePath $script:LogFile -Append
    }
}

# Logging function (define in global scope so modules can use it)
$script:LogFile = $null
if ($LogPath) {
    $script:LogFile = $LogPath
    if (-not (Test-Path (Split-Path $LogPath -Parent))) {
        New-Item -ItemType Directory -Path (Split-Path $LogPath -Parent) -Force | Out-Null
    }
    "=== Deployment Log Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') ===" | Out-File -FilePath $LogPath -Append
}

function global:Write-Log {
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

# Check for GitHub PAT (from parameter, environment variable, or prompt)
if (-not $GitHubPat) {
    $GitHubPat = $env:GITHUB_PAT
}

if (-not $GitHubPat) {
    Write-ColorOutput Yellow "ℹ️  No GitHub PAT found. Static Web App will be created without GitHub integration."
    Write-ColorOutput Yellow "   To add GitHub integration later, set GITHUB_PAT environment variable or use -GitHubPat parameter."
    Write-Output ""
    Write-ColorOutput Cyan "   To create a PAT:"
    Write-Output "   1. Go to: https://github.com/settings/tokens"
    Write-Output "   2. Generate new token (classic)"
    Write-Output "   3. Select scopes: repo, workflow, admin:repo_hook"
    Write-Output "   4. Set environment variable: `$env:GITHUB_PAT = 'your-token'"
    Write-Output ""
}
else {
    Write-ColorOutput Green "✅ GitHub PAT found. Will use for Static Web App GitHub integration."
    Write-Log "GitHub PAT provided for Static Web App integration" "INFO"
    # Validate PAT format (basic check)
    if ($GitHubPat.Length -lt 20) {
        Write-ColorOutput Yellow "⚠️  GitHub PAT seems too short. Please verify it's correct."
    }
}

# Check for SWA CLI (required for GitHub PAT support)
Write-ColorOutput Cyan "Checking for SWA CLI..."

# Check multiple ways: Get-Command, where.exe, and npm list
$swaCommand = Get-Command swa -ErrorAction SilentlyContinue
$swaPath = where.exe swa 2>$null
$swaNpmInstalled = npm list -g @azure/static-web-apps-cli --depth=0 2>$null | Select-String -Pattern "@azure/static-web-apps-cli"

if ($swaCommand -or $swaPath -or $swaNpmInstalled) {
    # Found it - determine which method worked
    if ($swaCommand) {
        Write-ColorOutput Green "✅ SWA CLI found at: $($swaCommand.Source)"
    }
    elseif ($swaPath) {
        Write-ColorOutput Green "✅ SWA CLI found at: $swaPath"
        $script:swaCliAvailable = $true
    }
    elseif ($swaNpmInstalled) {
        Write-ColorOutput Green "✅ SWA CLI is installed via npm"
        # Try to find it in npm global bin
        $npmGlobalBin = npm config get prefix 2>$null | ForEach-Object { if ($_) { Join-Path $_ "node_modules\@azure\static-web-apps-cli\dist\swa.cmd" } }
        if ($npmGlobalBin -and (Test-Path $npmGlobalBin)) {
            $script:swaCliAvailable = $true
            $script:swaCliPath = $npmGlobalBin
        }
        else {
            # Try common npm global locations
            $commonPaths = @(
                "$env:APPDATA\npm\swa.cmd",
                "$env:ProgramFiles\nodejs\swa.cmd",
                "$env:LOCALAPPDATA\npm\swa.cmd"
            )
            foreach ($path in $commonPaths) {
                if (Test-Path $path) {
                    $script:swaCliAvailable = $true
                    $script:swaCliPath = $path
                    break
                }
            }
        }
    }
    Write-Log "SWA CLI found" "INFO"
}
else {
    Write-ColorOutput Yellow "⚠️  SWA CLI not found. Installing..."
    Write-Log "SWA CLI not found, installing..." "INFO"
    
    # Check if npm is available
    $npmAvailable = Get-Command npm -ErrorAction SilentlyContinue
    if (-not $npmAvailable) {
        Write-ColorOutput Red "❌ npm is not installed. Please install Node.js first: https://nodejs.org/"
        Write-ColorOutput Yellow "   SWA CLI requires Node.js/npm. Static Web App will be created without GitHub integration."
        Write-Log "npm not found, cannot install SWA CLI" "WARN"
        $script:swaCliAvailable = $false
    }
    else {
        try {
            Write-Host "Installing SWA CLI (this may take a minute)... " -NoNewline
            $installOutput = npm install -g @azure/static-web-apps-cli 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓" -ForegroundColor Green
                Write-ColorOutput Green "✅ SWA CLI installed successfully!"
                Write-Log "SWA CLI installed successfully" "INFO"
                
                # Try to find it after installation - check npm global bin
                $npmPrefix = npm config get prefix 2>$null
                if ($npmPrefix) {
                    $swaPath1 = Join-Path $npmPrefix "node_modules\@azure\static-web-apps-cli\dist\swa.cmd"
                    $swaPath2 = Join-Path $npmPrefix "swa.cmd"
                    $swaPath3 = "$env:APPDATA\npm\swa.cmd"
                    
                    if (Test-Path $swaPath1) {
                        $script:swaCliAvailable = $true
                        $script:swaCliPath = $swaPath1
                    }
                    elseif (Test-Path $swaPath2) {
                        $script:swaCliAvailable = $true
                        $script:swaCliPath = $swaPath2
                    }
                    elseif (Test-Path $swaPath3) {
                        $script:swaCliAvailable = $true
                        $script:swaCliPath = $swaPath3
                    }
                    else {
                        # Force refresh PATH and try Get-Command again
                        $env:Path = [System.Environment]::GetEnvironmentVariable("Path", "Machine") + ";" + [System.Environment]::GetEnvironmentVariable("Path", "User")
                        Start-Sleep -Seconds 1
                        $swaCommand = Get-Command swa -ErrorAction SilentlyContinue
                        if ($swaCommand) {
                            $script:swaCliAvailable = $true
                            $script:swaCliPath = $swaCommand.Source
                        }
                        else {
                            $script:swaCliAvailable = $false
                            Write-ColorOutput Yellow "⚠️  SWA CLI installed but not found in PATH. You may need to restart your terminal."
                            Write-Log "SWA CLI installed but not in PATH" "WARN"
                        }
                    }
                }
                else {
                    $script:swaCliAvailable = $false
                    Write-ColorOutput Yellow "⚠️  SWA CLI installed but could not locate it. You may need to restart your terminal."
                    Write-Log "SWA CLI installed but could not locate" "WARN"
                }
            }
            else {
                Write-Host "✗" -ForegroundColor Red
                Write-ColorOutput Yellow "⚠️  Failed to install SWA CLI. Error: $($installOutput -join '`n')"
                Write-Log "Failed to install SWA CLI: $($installOutput -join '`n')" "WARN"
                Write-ColorOutput Yellow "   Static Web App will be created without GitHub integration."
                $script:swaCliAvailable = $false
            }
        }
        catch {
            Write-Host "✗" -ForegroundColor Red
            Write-ColorOutput Yellow "⚠️  Failed to install SWA CLI: $($_.Exception.Message)"
            Write-Log "Exception installing SWA CLI: $($_.Exception.Message)" "WARN"
            Write-ColorOutput Yellow "   Static Web App will be created without GitHub integration."
            $script:swaCliAvailable = $false
        }
    }
}

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

# Always check directly for the resource group (more reliable than scan)
Write-Log "Checking resource group $RG directly..." "INFO"
try {
    $rgInfo = az group show --name $RG 2>$null | ConvertFrom-Json
    if ($rgInfo) {
        $rgExists = $true
        Write-Log "Resource group $RG exists" "INFO"
        
        # Check for resources in the group
        $resources = az resource list --resource-group $RG --output json 2>$null | ConvertFrom-Json
        if ($resources -and $resources.Count -gt 0) {
            $hasResources = $true
            Write-Log "Found $($resources.Count) resources in resource group $RG" "INFO"
        }
        else {
            Write-Log "Resource group $RG exists but has no resources" "INFO"
        }
    }
}
catch {
    Write-Log "Resource group $RG does not exist or check failed: $($_.Exception.Message)" "INFO"
}

# Also check scan results as backup
$matchingRG = $existingResources.ResourceGroups | Where-Object { $_.Name -eq $RG }
if ($matchingRG -and -not $rgExists) {
    $rgExists = $true
    $hasResources = $matchingRG.HasResources
    Write-Log "Resource group $RG found via scan" "INFO"
}
elseif ($matchingRG -and $matchingRG.HasResources -and -not $hasResources) {
    # If scan says it has resources but direct check didn't find them, trust the scan
    $hasResources = $true
    Write-Log "Using scan result: resource group $RG has resources" "INFO"
}

# Check if Static Web App exists
$matchingSWA = $existingResources.StaticWebApps | Where-Object { $_.Name -eq $SWA_NAME -and $_.ResourceGroup -eq $RG }
if ($matchingSWA) {
    $swaExists = $true
    Write-Log "Static Web App $SWA_NAME found via scan" "INFO"
}
else {
    # Also check directly
    try {
        $swaInfo = az staticwebapp show --name $SWA_NAME --resource-group $RG 2>$null | ConvertFrom-Json
        if ($swaInfo) {
            $swaExists = $true
            Write-Log "Static Web App $SWA_NAME found via direct check" "INFO"
        }
    }
    catch {
        # SWA doesn't exist
        Write-Log "Static Web App $SWA_NAME does not exist" "INFO"
    }
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
    
    # Re-check resource group existence after user selection
    # Double-check resource group and resources directly (more reliable than scan)
    if (-not $rgExists) {
        try {
            $rgInfo = az group show --name $RG 2>$null | ConvertFrom-Json
            if ($rgInfo) {
                $rgExists = $true
                $resources = az resource list --resource-group $RG --output json 2>$null | ConvertFrom-Json
                if ($resources -and $resources.Count -gt 0) {
                    $hasResources = $true
                    Write-Log "Found $($resources.Count) resources in resource group $RG" "INFO"
                }
            }
        }
        catch {
            # Resource group doesn't exist
            Write-Log "Resource group $RG does not exist" "INFO"
        }
    }
}

# Check if infrastructure is deployed (either has resources in RG or SWA exists)
$infrastructureDeployed = ($hasResources -or $swaExists)

if ($infrastructureDeployed) {
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
    if ($Verbose) {
        Write-ColorOutput Yellow "No resources found in resource group '$RG'"
        Write-Output "   (rgExists: $rgExists, hasResources: $hasResources, swaExists: $swaExists)"
        Write-Output "   (matchingRG found: $($null -ne $matchingRG), matchingSWA found: $($null -ne $matchingSWA))"
    }
    else {
        Write-ColorOutput Yellow "No resources found"
    }
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
    
    # Try to reuse existing JWT secret from App Services
    $JWT_SECRET_BASE64 = $null
    $existingJwtSecret = $null
    
    # Check existing App Services in the resource group for JWT secret
    Write-ColorOutput Cyan "   Checking for existing JWT secret in App Services..."
    Write-Log "Checking for existing JWT secret in App Services" "INFO"
    
    try {
        # Get all App Services (sites) in the resource group
        $appServices = az resource list `
            --resource-group $RG `
            --resource-type "Microsoft.Web/sites" `
            --output json 2>$null | ConvertFrom-Json
        
        if ($appServices) {
            foreach ($appService in $appServices) {
                Write-ColorOutput Cyan "   Checking App Service '$($appService.name)'..."
                Write-Log "Checking App Service $($appService.name) for JWT secret" "INFO"
                
                try {
                    $appSettings = az webapp config appsettings list `
                        --name $appService.name `
                        --resource-group $RG `
                        --output json 2>$null | ConvertFrom-Json
                    
                    if ($appSettings) {
                        # Check for Jwt__Key (preferred) or JwtSettings__SecretKey
                        $jwtKey = $appSettings | Where-Object { $_.name -eq "Jwt__Key" -or $_.name -eq "JwtSettings__SecretKey" }
                        
                        if ($jwtKey -and $jwtKey.value) {
                            $existingJwtSecret = $jwtKey.value
                            Write-ColorOutput Green "   ✅ Found existing JWT secret in App Service '$($appService.name)'"
                            Write-Log "Found existing JWT secret in App Service $($appService.name)" "INFO"
                            break
                        }
                    }
                }
                catch {
                    Write-Log "Failed to retrieve app settings from $($appService.name): $($_.Exception.Message)" "WARN"
                }
            }
        }
    }
    catch {
        Write-Log "Failed to query App Services for JWT secret: $($_.Exception.Message)" "WARN"
    }
    
    # Use existing secret if found, otherwise generate new one
    if ($existingJwtSecret) {
        $JWT_SECRET_BASE64 = $existingJwtSecret
        Write-ColorOutput Green "✅ Reusing existing JWT secret"
        Write-Log "Reusing existing JWT secret from App Service" "INFO"
    }
    else {
        Write-ColorOutput Cyan "   Generating new JWT secret..."
        Write-Log "No existing JWT secret found, generating new one" "INFO"
        $JWT_SECRET = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | ForEach-Object { [char]$_ })
        $JWT_SECRET_BASE64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($JWT_SECRET))
    }
    
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
                    
                    # Create Static Web App
                    Write-Host "Creating Static Web App... " -NoNewline
                    Write-Log "Creating Static Web App: $SWA_NAME" "INFO"
                    
                    # Track if SWA was created successfully
                    $swaCreated = $false
                    $swaResult = $null
                    
                    # Use SWA CLI if available (better GitHub PAT support)
                    # Re-check in case it was just installed or path wasn't found earlier
                    if (-not $script:swaCliAvailable -or -not $script:swaCliPath) {
                        # Check common npm locations
                        $swaCheckPaths = @(
                            "$env:APPDATA\npm\swa.cmd",
                            "$env:LOCALAPPDATA\npm\swa.cmd"
                        )
                        $npmPrefix = npm config get prefix 2>$null
                        if ($npmPrefix) {
                            $swaCheckPaths += @(
                                Join-Path $npmPrefix "swa.cmd",
                                Join-Path $npmPrefix "node_modules\@azure\static-web-apps-cli\dist\swa.cmd"
                            )
                        }
                        
                        foreach ($checkPath in $swaCheckPaths) {
                            if ($checkPath -and (Test-Path $checkPath)) {
                                $script:swaCliAvailable = $true
                                $script:swaCliPath = $checkPath
                                # Add to PATH for this session
                                $swaDir = Split-Path $checkPath -Parent
                                if ($env:Path -notlike "*$swaDir*") {
                                    $env:Path = "$swaDir;$env:Path"
                                }
                                break
                            }
                        }
                        
                        # Also try Get-Command as fallback
                        if (-not $script:swaCliAvailable) {
                            $swaCheck = Get-Command swa -ErrorAction SilentlyContinue
                            if ($swaCheck) {
                                $script:swaCliAvailable = $true
                                $script:swaCliPath = $swaCheck.Source
                            }
                        }
                    }
                    
                    if ($script:swaCliAvailable -and $GitHubPat -and $repoOwner -and $repoName) {
                        # Use SWA CLI to create and connect (better GitHub PAT support)
                        Write-Output ""
                        Write-ColorOutput Cyan "   Using SWA CLI to create Static Web App with GitHub PAT..."
                        Write-Log "Using SWA CLI to create Static Web App with GitHub PAT" "INFO"
                        
                        # Static Web Apps have limited region support - check if current region is supported
                        $swaSupportedRegions = @("westus2", "centralus", "eastus2", "westeurope", "eastasia")
                        $swaLocation = $LOCATION
                        
                        if ($LOCATION -notin $swaSupportedRegions) {
                            Write-ColorOutput Yellow "   ⚠️  Static Web Apps not available in $LOCATION"
                            Write-ColorOutput Yellow "   Available regions: $($swaSupportedRegions -join ', ')"
                            Write-ColorOutput Cyan "   Creating Static Web App in 'westeurope' (closest supported region)"
                            Write-Log "Static Web Apps not available in $LOCATION, using westeurope" "WARN"
                            $swaLocation = "westeurope"
                        }
                        
                        # Use the stored path or find it again
                        $swaCmd = if ($script:swaCliPath -and (Test-Path $script:swaCliPath)) {
                            $script:swaCliPath
                        }
                        else {
                            # Try to find it in common locations
                            $foundPath = $null
                            $checkPaths = @(
                                "$env:APPDATA\npm\swa.cmd",
                                "$env:LOCALAPPDATA\npm\swa.cmd"
                            )
                            $npmPrefix = npm config get prefix 2>$null
                            if ($npmPrefix) {
                                $checkPaths += @(
                                    Join-Path $npmPrefix "swa.cmd",
                                    Join-Path $npmPrefix "node_modules\@azure\static-web-apps-cli\dist\swa.cmd"
                                )
                            }
                            foreach ($path in $checkPaths) {
                                if ($path -and (Test-Path $path)) {
                                    $foundPath = $path
                                    $script:swaCliPath = $path
                                    break
                                }
                            }
                            if ($foundPath) { $foundPath } else { "swa" }
                        }
                        
                        Write-Log "Using SWA CLI at: $swaCmd" "INFO"
                        
                        # Get subscription ID
                        $azAccount = az account show --output json 2>$null | ConvertFrom-Json
                        $subscriptionId = if ($azAccount) { $azAccount.id } else { $SubscriptionId }
                        
                        # Set GitHub token as environment variable for SWA CLI
                        $env:GITHUB_TOKEN = $GitHubPat
                        
                        # Use SWA CLI to create and connect the Static Web App
                        Write-ColorOutput Cyan "   Creating Static Web App with SWA CLI..."
                        Write-Log "Creating Static Web App: $SWA_NAME in resource group: $RG using SWA CLI" "INFO"
                        
                        # Ensure SWA CLI is logged in
                        Write-ColorOutput Cyan "   Authenticating with SWA CLI..."
                        $swaLoginOutput = & $swaCmd login 2>&1
                        if ($LASTEXITCODE -ne 0) {
                            Write-ColorOutput Yellow "   ⚠️  SWA CLI login may have failed, but continuing..."
                            Write-Log "SWA CLI login output: $($swaLoginOutput -join '`n')" "WARN"
                        }
                        
                        # Set GitHub token as environment variable for SWA CLI
                        $env:GITHUB_TOKEN = $GitHubPat
                        
                        # Use SWA CLI deploy to create the Static Web App and connect GitHub
                        # Create a minimal output directory for deployment
                        $tempDeployDir = Join-Path $env:TEMP "swa-deploy-$(Get-Random)"
                        New-Item -ItemType Directory -Path $tempDeployDir -Force | Out-Null
                        try {
                            # Create a minimal index.html for deployment
                            "<!DOCTYPE html><html><head><title>SWA Setup</title></head><body><h1>Setting up Static Web App</h1></body></html>" | Out-File -FilePath (Join-Path $tempDeployDir "index.html") -Encoding UTF8
                            
                            Write-ColorOutput Cyan "   Creating Static Web App and connecting to GitHub with SWA CLI..."
                            Write-Log "Using SWA CLI deploy to create SWA and connect GitHub" "INFO"
                            
                            # Use SWA CLI deploy with all parameters - it should create the resource if it doesn't exist
                            $swaDeployOutput = & $swaCmd deploy $tempDeployDir `
                                --app-name $SWA_NAME `
                                --resource-group $RG `
                                --subscription-id $subscriptionId `
                                --repo "https://github.com/$repoOwner/$repoName" `
                                --branch "dev" `
                                --app-location "./src/Mystira.App.PWA" `
                                --api-location "swa-db-connections" `
                                --output-location "out" `
                                2>&1
                            
                            if ($LASTEXITCODE -eq 0) {
                                Write-Host "✓" -ForegroundColor Green
                                Write-ColorOutput Green "✅ Static Web App created and connected to GitHub via SWA CLI!"
                                Write-Log "Static Web App created and connected via SWA CLI" "INFO"
                                $swaCreated = $true
                                # Set a dummy result so the success check works
                                $swaResult = @{ name = $SWA_NAME }
                            }
                            else {
                                # If deploy fails, try creating resource first with Azure CLI, then connecting
                                $errorOutput = $swaDeployOutput -join '`n'
                                Write-ColorOutput Yellow "   ⚠️  SWA CLI deploy failed. Creating resource first..."
                                Write-Log "SWA CLI deploy failed, creating resource first: $errorOutput" "WARN"
                                
                                # Check if error is due to region unavailability
                                $isRegionError = $false
                                if ($errorOutput -match "LocationNotAvailableForResourceType|region.*not available|not available.*region") {
                                    $isRegionError = $true
                                }
                                
                                # If region error and we haven't already switched, prompt for region change
                                if ($isRegionError -and $swaLocation -eq $LOCATION) {
                                    Write-Output ""
                                    Write-ColorOutput Yellow "⚠️  Static Web Apps are not available in region: $LOCATION"
                                    Write-ColorOutput Yellow "   Available regions: $($swaSupportedRegions -join ', ')"
                                    Write-Output ""
                                    
                                    $changeRegion = Read-Host "Would you like to create the Static Web App in a different region? (y/n)"
                                    if ($changeRegion -eq 'y' -or $changeRegion -eq 'Y') {
                                        Write-Output ""
                                        Write-Output "Available Static Web App regions:"
                                        for ($i = 0; $i -lt $swaSupportedRegions.Count; $i++) {
                                            Write-Output "  [$($i+1)] $($swaSupportedRegions[$i])"
                                        }
                                        Write-Output ""
                                        
                                        do {
                                            $regionChoice = Read-Host "Enter region number (1-$($swaSupportedRegions.Count))"
                                            if ($regionChoice -notmatch "^\d+$" -or [int]$regionChoice -lt 1 -or [int]$regionChoice -gt $swaSupportedRegions.Count) {
                                                Write-ColorOutput Red "❌ Invalid choice. Please enter a number between 1 and $($swaSupportedRegions.Count)."
                                            }
                                        } while ($regionChoice -notmatch "^\d+$" -or [int]$regionChoice -lt 1 -or [int]$regionChoice -gt $swaSupportedRegions.Count)
                                        
                                        $swaLocation = $swaSupportedRegions[[int]$regionChoice - 1]
                                        Write-ColorOutput Green "✅ Will create Static Web App in: $swaLocation"
                                        Write-Log "User selected region: $swaLocation" "INFO"
                                    }
                                    else {
                                        Write-ColorOutput Yellow "   Skipping Static Web App creation."
                                        Write-Log "User declined region change, skipping SWA creation" "WARN"
                                        break
                                    }
                                }
                                
                                # Try creating with Azure CLI
                                $swaCreateOutput = az staticwebapp create `
                                    --name $SWA_NAME `
                                    --resource-group $RG `
                                    --location $swaLocation `
                                    --sku Free `
                                    --output json 2>&1
                                
                                if ($LASTEXITCODE -eq 0) {
                                    Write-ColorOutput Green "   ✅ Resource created in $swaLocation. Connecting to GitHub..."
                                    
                                    # Connect with REST API
                                    try {
                                        $accessToken = az account get-access-token --resource "https://management.azure.com" --query accessToken -o tsv 2>$null
                                        
                                        if ($accessToken -and $azAccount) {
                                            $apiVersion = "2022-03-01"
                                            $uri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$RG/providers/Microsoft.Web/staticSites/$SWA_NAME/sourcecontrols/GitHub?api-version=$apiVersion"
                                            
                                            $body = @{
                                                properties = @{
                                                    repo                      = "https://github.com/$repoOwner/$repoName"
                                                    branch                    = "dev"
                                                    githubActionConfiguration = @{
                                                        generateWorkflowFile = $true
                                                        workflowSettings     = @{
                                                            appLocation    = "./src/Mystira.App.PWA"
                                                            apiLocation    = "swa-db-connections"
                                                            outputLocation = "out"
                                                        }
                                                    }
                                                    githubPersonalAccessToken = $GitHubPat
                                                }
                                            } | ConvertTo-Json -Depth 10
                                            
                                            $headers = @{
                                                "Authorization" = "Bearer $accessToken"
                                                "Content-Type"  = "application/json"
                                            }
                                            
                                            $response = Invoke-RestMethod -Uri $uri -Method Put -Headers $headers -Body $body -ErrorAction Stop
                                            
                                            Write-Host "✓" -ForegroundColor Green
                                            Write-ColorOutput Green "✅ Static Web App created and connected to GitHub!"
                                            Write-Log "Static Web App created and connected via REST API" "INFO"
                                            $swaCreated = $true
                                            # Set result so success check works
                                            $swaResult = @{ name = $SWA_NAME }
                                        }
                                        else {
                                            Write-ColorOutput Yellow "   ⚠️  Could not get Azure access token."
                                        }
                                    }
                                    catch {
                                        Write-ColorOutput Yellow "   ⚠️  Failed to connect to GitHub: $($_.Exception.Message)"
                                        Write-Log "Failed to connect GitHub: $($_.Exception.Message)" "WARN"
                                    }
                                }
                                else {
                                    $errorMsg = $swaCreateOutput -join '`n'
                                    
                                    # Check if it's still a region error
                                    if ($errorMsg -match "LocationNotAvailableForResourceType" -and $swaLocation -ne $LOCATION) {
                                        Write-Host "✗" -ForegroundColor Red
                                        Write-ColorOutput Red "   ❌ Static Web Apps are not available in $swaLocation either."
                                        Write-ColorOutput Yellow "   Error: $errorMsg"
                                        Write-Log "Static Web App creation failed in ${swaLocation}: $errorMsg" "ERROR"
                                    }
                                    else {
                                        Write-Host "✗" -ForegroundColor Red
                                        Write-ColorOutput Yellow "   ⚠️  Static Web App creation failed."
                                        Write-ColorOutput Yellow "   Error: $errorMsg"
                                        Write-Log "Static Web App creation failed: $errorMsg" "ERROR"
                                    }
                                }
                            }
                        }
                        finally {
                            # Clean up temp directory
                            if (Test-Path $tempDeployDir) {
                                Remove-Item -Path $tempDeployDir -Recurse -Force -ErrorAction SilentlyContinue
                            }
                        }
                    }
                    else {
                        # Use Azure CLI (fallback)
                        Write-Output ""
                        if (-not $swaCliAvailable) {
                            Write-ColorOutput Yellow "   ℹ️  SWA CLI not found. Using Azure CLI (install with: npm install -g @azure/static-web-apps-cli)"
                        }
                        
                        $swaResult = az staticwebapp create `
                            --name $SWA_NAME `
                            --resource-group $RG `
                            --location $LOCATION `
                            --sku Free `
                            --output json 2>$null | ConvertFrom-Json
                        
                        if ($swaResult) {
                            Write-Host "✓" -ForegroundColor Green
                            Write-ColorOutput Green "✅ Static Web App created!"
                            if (-not $GitHubPat) {
                                Write-ColorOutput Yellow "   ⚠️  Created without GitHub integration."
                                Write-ColorOutput Yellow "   Connect it to GitHub later via Azure Portal or use:"
                                Write-ColorOutput Cyan "   az staticwebapp connect --name $SWA_NAME --resource-group $RG --login-with-github"
                            }
                            else {
                                Write-ColorOutput Yellow "   ⚠️  Install SWA CLI for better GitHub PAT support: npm install -g @azure/static-web-apps-cli"
                            }
                        }
                    }
                    
                    if ($swaCreated -or $swaResult) {
                        if (-not $swaCreated) {
                            Write-Host "✓" -ForegroundColor Green
                            Write-ColorOutput Green "✅ Static Web App created!"
                            Write-Log "Static Web App created successfully: $SWA_NAME" "INFO"
                        }
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
