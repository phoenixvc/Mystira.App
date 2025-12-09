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
    Import-Module (Join-Path $deployModulePath "StaticWebAppHelpers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "SecretHelpers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "GitHelpers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "ResourceInfoHelpers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "ResourceGroupHelpers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "BicepHelpers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "ResourceSummaryHelpers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "InfrastructureDetectionHelpers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "ResourceSelectionHelpers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "StaticWebAppCreationHelpers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "DeploymentOrchestrationHelpers.psm1") -Force
    Import-Module (Join-Path $deployModulePath "DeploymentStateHelpers.psm1") -Force
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

# Scan for existing resources
$existingResources = Get-ExistingResources -SkipScan:$SkipScan -Verbose:$Verbose -TimeoutSeconds $script:AZURE_CLI_TIMEOUT_SECONDS

$RG = Get-ResourceGroupName $LOCATION
$SWA_NAME = Get-StaticWebAppName $LOCATION

# Check infrastructure status using helper function
$infrastructureStatus = Get-InfrastructureStatus `
    -Location $LOCATION `
    -ResourceGroup $RG `
    -StaticWebAppName $SWA_NAME `
    -ExistingResources $existingResources `
    -Verbose:$Verbose

$rgExists = $infrastructureStatus.ResourceGroupExists
$hasResources = $infrastructureStatus.HasResources
$swaExists = $infrastructureStatus.StaticWebAppExists

# If we have existing resources, show them and ask what to do
if ($existingResources.ResourceGroups.Count -gt 0 -or $existingResources.StaticWebApps.Count -gt 0) {
    $selection = Show-ResourceSelectionMenu `
        -ExistingResources $existingResources `
        -DefaultResourceGroup $RG `
        -DefaultLocation $LOCATION `
        -AvailableRegions $REGIONS
    
    # Apply selection
    switch ($selection.Action) {
        "use_selected" {
            $RG = $selection.ResourceGroup
            $LOCATION = $selection.Location
            $SWA_NAME = $selection.StaticWebAppName
        }
        "create_new" {
            $rgExists = $false
            $hasResources = $false
        }
        "create_in_region" {
            $RG = $selection.ResourceGroup
            $LOCATION = $selection.Location
            $SWA_NAME = $selection.StaticWebAppName
            $rgExists = $false
            $hasResources = $false
        }
    }
    
    Write-Output ""
    
    # Re-check infrastructure status after selection
    $infrastructureStatus = Get-InfrastructureStatus `
        -Location $LOCATION `
        -ResourceGroup $RG `
        -StaticWebAppName $SWA_NAME `
        -ExistingResources $existingResources `
        -Verbose:$Verbose
    
    $rgExists = $infrastructureStatus.ResourceGroupExists
    $hasResources = $infrastructureStatus.HasResources
    $swaExists = $infrastructureStatus.StaticWebAppExists
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
    
    $gitStatus = Get-GitRepositoryStatus
    $CURRENT_BRANCH = $gitStatus.Branch
    $TARGET_BRANCH = if ($Branch) { $Branch } else { $CURRENT_BRANCH }
    Write-Log "Current branch: $CURRENT_BRANCH, Target branch: $TARGET_BRANCH" "INFO"
    
    # Check if there are uncommitted changes
    Write-Log "Step: Checking for uncommitted changes" "INFO"
    if ($gitStatus.HasUncommittedChanges) {
        Write-ColorOutput Yellow "⚠️  You have uncommitted changes."
        $response = Read-Host "Do you want to commit them? (y/n)"
        if ($response -eq 'y' -or $response -eq 'Y') {
            git add .
            Commit-GitChanges -Message $Message
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
            $CURRENT_BRANCH = $TARGET_BRANCH
        }
        else {
            Write-ColorOutput Red "❌ Aborting."
            exit 1
        }
    }
    
    # Sync repository (fetch and pull if needed)
    Write-ColorOutput Yellow "📥 Fetching latest changes..."
    Sync-GitRepository -Branch $TARGET_BRANCH
    
    # Check if we're ahead of remote
    $LOCAL_COMMITS = git rev-list origin/$TARGET_BRANCH..HEAD --count 2>$null
    if ($LOCAL_COMMITS -eq 0) {
        # Create an empty commit to trigger deployment
        Write-ColorOutput Yellow "📝 Creating empty commit to trigger deployment..."
        Commit-GitChanges -Message $Message -AllowEmpty
    }
    
    # Push to remote
    Write-ColorOutput Yellow "📤 Pushing to origin/$TARGET_BRANCH..."
    Push-GitBranch -Branch $TARGET_BRANCH
    
    Write-Output ""
    Write-ColorOutput Green "✅ Code deployment triggered!"
    Write-ColorOutput Green "   Branch: $TARGET_BRANCH"
    Write-ColorOutput Green "   Check GitHub Actions for deployment status"
    Write-Output ""
    
    # Check if Static Web App exists, create if missing
    Write-Log "Step: Checking if Static Web App needs to be created" "INFO"
    $swaCheck = $false
    try {
        $swaInfo = Get-StaticWebAppInfo -Name $SWA_NAME -ResourceGroup $RG
        # Validate it's actually a SWA (has name property and no error)
        if ($swaInfo -and $swaInfo.name -and -not $swaInfo.error) {
            $swaCheck = $true
            Write-Log "Static Web App $SWA_NAME already exists" "INFO"
            Write-ColorOutput Green "SUCCESS: Static Web App already exists: $SWA_NAME"

            # Offer to disconnect built-in CI/CD to allow GitHub Actions to take over
            Write-Output ""
            $disconnectSwa = Read-Host "Disconnect built-in CI/CD to use GitHub Actions instead? (y/n)"
            if ($disconnectSwa -eq 'y' -or $disconnectSwa -eq 'Y') {
                Write-Host "Disconnecting SWA built-in CI/CD... " -NoNewline
                $disconnectResult = az staticwebapp disconnect --name $SWA_NAME --resource-group $RG 2>&1
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "✓" -ForegroundColor Green
                    Write-ColorOutput Yellow "   Get deployment token for GitHub Actions:"
                    Write-ColorOutput Cyan "   az staticwebapp secrets list --name $SWA_NAME --resource-group $RG --query properties.apiKey -o tsv"
                }
                else {
                    Write-Host "✗" -ForegroundColor Red
                    Write-Output "   (May already be disconnected: $disconnectResult)"
                }
            }
        }
        else {
            Write-Log "Static Web App $SWA_NAME does not exist, will create it" "INFO"
        }
    }
    catch {
        # Static Web App doesn't exist
        Write-Log "Static Web App $SWA_NAME does not exist, will create it" "INFO"
    }
    
    if (-not $swaCheck) {
        Write-Output ""
        Write-ColorOutput Cyan "📱 Static Web App not found. Would you like to create it now?"
        Write-Output ""
        
        $createSWA = Read-Host "Create Static Web App '$SWA_NAME'? (y/n)"
        if ($createSWA -eq 'y' -or $createSWA -eq 'Y') {
            Write-Output ""
            Write-ColorOutput Cyan "Creating Static Web App..."
            Write-Log "Creating Static Web App: $SWA_NAME" "INFO"
            
            # Get subscription ID
            $azAccount = az account show --output json 2>$null | ConvertFrom-Json
            $subscriptionId = if ($azAccount) { $azAccount.id } else { $SubscriptionId }
            
            # Create Static Web App using module function
            Write-Host "Creating Static Web App... " -NoNewline
            $swaResult = New-StaticWebAppWithGitHub `
                -Name $SWA_NAME `
                -ResourceGroup $RG `
                -Location $LOCATION `
                -SubscriptionId $subscriptionId `
                -GitHubPat $GitHubPat `
                -SwaCliPath $script:swaCliPath `
                -SwaCliAvailable:$script:swaCliAvailable `
                -Verbose:$Verbose
            
            if ($swaResult.Success) {
                Write-Host "[OK]" -ForegroundColor Green
                Write-ColorOutput Green "SUCCESS: Static Web App created!"
                if ($swaResult.Created) {
                    Write-Output ""
                    Write-ColorOutput Yellow "WARNING: IMPORTANT: Get the deployment token and add it to GitHub Secrets:"
                    Write-Output ""
                    Write-Output "   Secret name: AZURE_STATIC_WEB_APPS_API_TOKEN_DEV_MYSTIRA_APP"
                    Write-Output ""
                    Write-Output "   Get token with:"
                    Write-ColorOutput Cyan "   az staticwebapp secrets list --name $SWA_NAME --resource-group $RG --query properties.apiKey -o tsv"
                    Write-Output ""
                }
            }
            else {
                # Verify if SWA was actually created despite the error (safety check)
                Write-Log "SWA creation reported failure, verifying if resource exists..." "WARN"
                $swaExists = Test-StaticWebAppExists -Name $SWA_NAME -ResourceGroup $RG
                if ($swaExists) {
                    Write-Host "[OK]" -ForegroundColor Green
                    Write-ColorOutput Green "SUCCESS: Static Web App was created successfully!"
                    Write-ColorOutput Yellow "   (Creation reported an error, but the resource exists in Azure)"
                    Write-Log "SWA exists despite reported error - creation succeeded" "INFO"
                }
                else {
                    Write-Host "[X]" -ForegroundColor Red
                    Write-ColorOutput Yellow "WARNING: Static Web App creation failed: $($swaResult.Error)"
                    Write-Log "Static Web App creation failed: $($swaResult.Error)" "ERROR"
                }
            }
            else {
                Write-ColorOutput Yellow "   Skipping Static Web App creation."
            }
        }
        Write-Output ""
    }
    
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
        $rgResult = New-ResourceGroup -Name $RG -Location $LOCATION
        if ($rgResult.Success) {
            Write-Host "✓" -ForegroundColor Green
        }
        else {
            Write-Host "✗" -ForegroundColor Red
            Write-ColorOutput Red "❌ Failed to create resource group: $($rgResult.Error)"
            exit 1
        }
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
    Write-ColorOutput Cyan "   Checking for existing JWT secret in App Services..."
    Write-Log "Checking for existing JWT secret in App Services" "INFO"
    
    $existingJwtSecret = Get-ExistingJwtSecret -ResourceGroup $RG
    
    # Use existing secret if found, otherwise generate new one
    if ($existingJwtSecret) {
        $JWT_SECRET_BASE64 = $existingJwtSecret
        Write-ColorOutput Green "SUCCESS: Reusing existing JWT secret"
        Write-Log "Reusing existing JWT secret from App Service" "INFO"
    }
    else {
        Write-ColorOutput Cyan "   Generating new JWT secret..."
        Write-Log "No existing JWT secret found, generating new one" "INFO"
        $jwtSecretObj = New-JwtSecret -Length 32
        $JWT_SECRET_BASE64 = $jwtSecretObj.Base64
    }
    
    # Get resource prefix and calculate expected storage name
    $resourcePrefix = Get-ResourcePrefix -Location $LOCATION
    $expectedStorageName = Get-ExpectedStorageAccountName -ResourcePrefix $resourcePrefix
    
    Write-ColorOutput Cyan "🚀 Deploying infrastructure..."
    Write-Output "   (Will handle conflicts if they occur)"
    Write-Output ""
    
    # Initialize rollback tracking
    $deployName = "mystira-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Initialize-RollbackTracking -ResourceGroup $RG -DeploymentName $deployName
    
    # Deploy infrastructure using orchestration function
    $deploymentResult = Invoke-InfrastructureDeploymentWithRetry `
        -ResourceGroup $RG `
        -Location $LOCATION `
        -BicepFile $BICEP_FILE `
        -ResourcePrefix $resourcePrefix `
        -ExpectedStorageName $expectedStorageName `
        -JwtSecretBase64 $JWT_SECRET_BASE64 `
        -DeploymentName $deployName `
        -MaxRetries $script:MAX_DEPLOYMENT_RETRIES `
        -Verbose:$Verbose `
        -WhatIf:$WhatIf
    
    # Update variables from result (in case region/resource group changed)
    $LOCATION = $deploymentResult.FinalLocation
    $RG = $deploymentResult.FinalResourceGroup
    $resourcePrefix = $deploymentResult.FinalResourcePrefix
    $expectedStorageName = $deploymentResult.FinalExpectedStorageName
    
    # Handle deployment result
    if (-not $deploymentResult.Success) {
        # Check if we need to offer rollback
        $createdResources = Get-CreatedResources
        if ($createdResources.Resources.Count -gt 0) {
            Write-Output ""
            Write-ColorOutput Yellow "WARNING: Would you like to rollback created resources? (y/n)"
            $rollbackChoice = Read-Host
            if ($rollbackChoice -eq 'y' -or $rollbackChoice -eq 'Y') {
                Invoke-Rollback -Confirm:$false
            }
        }
        
        Write-Output ""
        Write-ColorOutput Red "ERROR: Deployment failed at step: Infrastructure deployment"
        Write-Log "Deployment failed at step: Infrastructure deployment" "ERROR"
        Write-Output ""
        Write-Output "Error: $($deploymentResult.Error)"
        Write-Log "Error message: $($deploymentResult.Error)" "ERROR"
        exit 1
    }
    
    # Deployment succeeded - continue with post-deployment steps
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
    $swaCheck = Test-StaticWebAppExists -Name $SWA_NAME -ResourceGroup $RG
    if ($swaCheck) {
        Write-Log "Static Web App already exists: $SWA_NAME" "INFO"
    }
    else {
        Write-Log "Static Web App not found: $SWA_NAME" "INFO"
    }
    
    if (-not $swaCheck) {
        Write-Log "Step: Creating Static Web App" "INFO"
        Write-ColorOutput Yellow "Creating Static Web App..."
        Write-Output ""
        
        # Get subscription ID
        $azAccount = az account show --output json 2>$null | ConvertFrom-Json
        $subscriptionId = if ($azAccount) { $azAccount.id } else { $SubscriptionId }
        
        # Create Static Web App using module function
        Write-Host "Creating Static Web App... " -NoNewline
        $swaResult = New-StaticWebAppWithGitHub `
            -Name $SWA_NAME `
            -ResourceGroup $RG `
            -Location $LOCATION `
            -SubscriptionId $subscriptionId `
            -GitHubPat $GitHubPat `
            -SwaCliPath $script:swaCliPath `
            -SwaCliAvailable:$script:swaCliAvailable `
            -Verbose:$Verbose
        
        if ($swaResult.Success) {
            Write-Host "[OK]" -ForegroundColor Green
            Write-ColorOutput Green "SUCCESS: Static Web App created!"
            if ($swaResult.Created) {
                Write-Output ""
                Write-ColorOutput Yellow "WARNING: IMPORTANT: Get the deployment token and add it to GitHub Secrets:"
                Write-Output ""
                Write-Output "   Secret name: AZURE_STATIC_WEB_APPS_API_TOKEN_DEV_MYSTIRA_APP"
                Write-Output ""
                Write-Output "   Get token with:"
                Write-ColorOutput Cyan "   az staticwebapp secrets list --name $SWA_NAME --resource-group $RG --query properties.apiKey -o tsv"
                Write-Output ""
            }
        }
        else {
            # Verify if SWA was actually created despite the error (safety check)
            Write-Log "SWA creation reported failure, verifying if resource exists..." "WARN"
            $swaExists = Test-StaticWebAppExists -Name $SWA_NAME -ResourceGroup $RG
            if ($swaExists) {
                Write-Host "[OK]" -ForegroundColor Green
                Write-ColorOutput Green "SUCCESS: Static Web App was created successfully!"
                Write-ColorOutput Yellow "   (Creation reported an error, but the resource exists in Azure)"
                Write-Log "SWA exists despite reported error - creation succeeded" "INFO"
            }
            else {
                Write-Host "[X]" -ForegroundColor Red
                Write-ColorOutput Yellow "WARNING: Static Web App creation failed: $($swaResult.Error)"
                Write-Log "Static Web App creation failed: $($swaResult.Error)" "ERROR"
            }
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