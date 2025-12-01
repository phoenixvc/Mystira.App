# Deploy Now Script - Smart deployment
# Checks if Azure resources exist:
#   - If NO → Deploy infrastructure
#   - If YES → Push code to trigger CI/CD
# Usage: .\.deploy-now.ps1 [region] [branch] [message]

param(
    [string]$Region = "",
    [string]$Branch = "",
    [string]$Message = "Trigger deployment"
)

# Colors for output
function Write-ColorOutput($ForegroundColor, $Message) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $host.UI.RawUI.ForegroundColor = $fc
}

# Azure config
$SUB = "22f9eb18-6553-4b7d-9451-47d0195085fe"
$REGIONS = @("eastus2", "westus2", "centralus", "westeurope", "northeurope", "eastasia")
$LOCATION = if ($Region) { $Region } else { $REGIONS[0] }

# Get resource group name from location
function Get-ResourceGroupName($loc) {
    switch ($loc) {
        "eastus2" { return "dev-eus2-rg-mystira-app" }
        "westus2" { return "dev-wus2-rg-mystira-app" }
        "centralus" { return "dev-cus-rg-mystira-app" }
        "westeurope" { return "dev-euw-rg-mystira-app" }
        "northeurope" { return "dev-eun-rg-mystira-app" }
        "eastasia" { return "dev-ea-rg-mystira-app" }
        default { return "dev-$($loc.Substring(0,4))-rg-mystira-app" }
    }
}

# Get Static Web App name based on location
function Get-StaticWebAppName($loc) {
    switch ($loc) {
        "eastus2" { return "dev-eus2-swa-mystira-app" }
        "westus2" { return "dev-wus2-swa-mystira-app" }
        "centralus" { return "dev-cus-swa-mystira-app" }
        "westeurope" { return "dev-euw-swa-mystira-app" }
        "northeurope" { return "dev-eun-swa-mystira-app" }
        "eastasia" { return "dev-ea-swa-mystira-app" }
        default { return "dev-$($loc.Substring(0,4))-swa-mystira-app" }
    }
}

Write-ColorOutput Cyan "🚀 Deploy Now - Smart Deployment"
Write-Output "================================="
Write-Output ""

# Check Azure login
Write-Host "Checking Azure login... " -NoNewline
try {
    $account = az account show 2>$null | ConvertFrom-Json
    if (-not $account) {
        throw "Not logged in"
    }
    Write-Host "✓" -ForegroundColor Green
    Write-Output "  ($($account.name))"
}
catch {
    Write-Host "✗" -ForegroundColor Red
    Write-Output "Logging in with device code..."
    az login --use-device-code
}

# Set subscription
Write-Host "Setting subscription... " -NoNewline
try {
    az account set --subscription $SUB 2>$null | Out-Null
    Write-Host "✓" -ForegroundColor Green
}
catch {
    Write-Host "✗" -ForegroundColor Red
    Write-Output "Available subscriptions:"
    az account list --output table
    exit 1
}

# Check if resource group exists and has resources
Write-Host "Checking infrastructure... " -NoNewline
$rgExists = $false
$hasResources = $false

try {
    $rgInfo = az group show --name $RG 2>$null | ConvertFrom-Json
    if ($rgInfo) {
        $rgExists = $true
        # Check if resource group has any resources
        $resources = az resource list --resource-group $RG --output json 2>$null | ConvertFrom-Json
        if ($resources -and $resources.Count -gt 0) {
            $hasResources = $true
        }
    }
}
catch {
    # Resource group doesn't exist
}
Write-Host "✓" -ForegroundColor Green

$RG = Get-ResourceGroupName $LOCATION
$SWA_NAME = Get-StaticWebAppName $LOCATION

# Check if Static Web App exists
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

if ($hasResources -and $swaExists) {
    Write-ColorOutput Green "✅ Infrastructure is deployed. Pushing code to trigger CI/CD..."
    Write-Output ""
    
    # Code deployment path
    $CURRENT_BRANCH = git rev-parse --abbrev-ref HEAD
    $TARGET_BRANCH = if ($Branch) { $Branch } else { $CURRENT_BRANCH }
    
    # Check if there are uncommitted changes
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
    } else {
        $expectedStorageName = $storageNameBase
    }
    
    Write-ColorOutput Cyan "🚀 Deploying infrastructure..."
    Write-Output "   (Will handle conflicts if they occur)"
    Write-Output ""
    
    # Deploy infrastructure - try first, handle conflicts on failure
    $retryCount = 0
    $maxRetries = 3
    $script:cosmosUseAttempted = $false
    $script:skipCosmosCreation = $false
    $script:skipStorageCreation = $false
    $script:skipCommServiceCreation = $false
    $script:skipAppServiceCreation = $false
    $script:appServiceUseAttempted = $false
    if (-not $script:existingAppServiceResourceGroup) { $script:existingAppServiceResourceGroup = "" }
    if (-not $script:existingCosmosResourceGroup) { $script:existingCosmosResourceGroup = "" }
    if (-not $script:existingCosmosDbAccountName) { $script:existingCosmosDbAccountName = "" }
    
    while ($retryCount -le $maxRetries) {
        # Ensure resource group exists in current region
        $rgCheck = az group show --name $RG 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
        if (-not $rgCheck) {
            Write-Host "Creating resource group $RG in $LOCATION..." -NoNewline
            az group create --name $RG --location $LOCATION --output none 2>$null
            Write-Host " ✓" -ForegroundColor Green
        }
        
        $deployName = "mystira-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        
        # Create parameters JSON file for secure parameters
        $paramsFile = Join-Path $env:TEMP "bicep-params-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
        
        # Build parameters object - preserve skip flags across retries using script-scoped variables
        # Debug: Show script-scoped variable values
        if ($retryCount -gt 0) {
            Write-ColorOutput Yellow "   Debug (retry $retryCount): script:skipAppServiceCreation=$($script:skipAppServiceCreation), script:existingAppServiceResourceGroup=$($script:existingAppServiceResourceGroup)"
        }
        
        $paramsObj = @{
            environment = @{ value = "dev" }
            location = @{ value = $LOCATION }
            resourcePrefix = @{ value = $resourcePrefix }
            jwtSecretKey = @{ value = $JWT_SECRET_BASE64 }
            newStorageAccountName = @{ value = $expectedStorageName }
            skipCosmosCreation = @{ value = [bool]$script:skipCosmosCreation }
            skipStorageCreation = @{ value = [bool]$script:skipStorageCreation }
            skipCommServiceCreation = @{ value = [bool]$script:skipCommServiceCreation }
            skipAppServiceCreation = @{ value = [bool]$script:skipAppServiceCreation }
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
        Write-ColorOutput Cyan "🚀 Starting infrastructure deployment..."
        Write-Output "This may take several minutes. Please wait..."
        Write-Output ""
        
        $deploymentSuccess = $false
        $shouldRetry = $false
        
        try {
            # Debug: Show parameters file contents
            Write-ColorOutput Yellow "   Debug: Parameters file contents:"
            $paramsDebug = Get-Content $paramsFile | ConvertFrom-Json
            Write-Output ($paramsDebug.parameters | ConvertTo-Json -Depth 10)
            Write-Output ""
            
            # Capture both stdout and stderr separately
            $stdout = az deployment group create `
                --resource-group $RG `
                --template-file $BICEP_FILE `
                --parameters "@$paramsFile" `
                --mode Incremental `
                --name $deployName `
                --output json 2>&1 | Where-Object { $_ -notmatch '^WARNING:' }
            
            if ($LASTEXITCODE -eq 0) {
                # Success - try to parse JSON
                try {
                    $result = $stdout | ConvertFrom-Json -ErrorAction Stop
                    Write-Output ($result | ConvertTo-Json -Depth 10 | Out-String)
                    $deploymentSuccess = $true
                } catch {
                    # If JSON parse fails but exit code is 0, deployment might have succeeded
                    Write-Output $stdout
                    $deploymentSuccess = $true
                }
            } else {
                # Failure - try to parse error JSON
                $errorJson = $null
                $errorMsg = ""
                
                try {
                    $errorJson = $stdout | ConvertFrom-Json -ErrorAction Stop
                    if ($errorJson.error) {
                        $errorMsg = $errorJson.error.message
                    }
                } catch {
                    # Not JSON, use raw output
                    $errorMsg = ($stdout -join "`n")
                }
                
                if (-not $errorMsg) {
                    $errorMsg = ($stdout -join "`n")
                }
                
                # If we already tried "use" for Communication Service and it failed again, don't retry
                if ($script:commServiceUseAttempted -and $errorMsg -match "NameReservationTaken" -and $errorMsg -match "communication") {
                    Write-Output ""
                    Write-ColorOutput Red "❌ Communication Service 'use' failed. Cannot proceed."
                    Write-Output "Error: $errorMsg"
                    exit 1
                }
                
                # Check for App Service errors FIRST (before handling other conflicts)
                # If we already tried using existing App Services in a PREVIOUS retry and it failed again, don't retry
                if ($script:appServiceUseAttempted -and ($errorMsg -match "Website.*already exists" -or ($errorMsg -match "Conflict" -and $errorMsg -match "Website"))) {
                    Write-Output ""
                    Write-ColorOutput Red "❌ App Service 'use existing' failed. Cannot proceed."
                    Write-ColorOutput Yellow "   The App Services exist but the deployment is still trying to create them."
                    Write-ColorOutput Cyan "   This may indicate the skipAppServiceCreation parameter isn't being applied correctly."
                    Write-ColorOutput Cyan "   Parameter should be: skipAppServiceCreation=true, existingAppServiceResourceGroup=$RG"
                    Write-Output "Error: $errorMsg"
                    exit 1
                }
                
                # Handle App Service conflicts (they already exist - provide detailed info and options)
                if ($errorMsg -match "Website.*already exists" -or ($errorMsg -match "Conflict" -and $errorMsg -match "Website")) {
                    Write-Output ""
                    Write-ColorOutput Yellow "⚠️  App Service conflict detected!"
                    Write-Output ""
                    Write-ColorOutput Cyan "   The following App Services already exist:"
                    $appServicesToDelete = @()
                    $apiAppServiceName = "${resourcePrefix}-app-mystira-api"
                    $adminApiAppServiceName = "${resourcePrefix}-app-mystira-admin-api"
                    
                    if ($errorMsg -match $apiAppServiceName -or $errorMsg -match "dev-euw-app-mystira-api") {
                        Write-ColorOutput Yellow "   • API App Service: $apiAppServiceName"
                        # Get App Service details
                        try {
                            $apiAppInfo = az webapp show --name $apiAppServiceName --resource-group $RG --query "{State:state, Location:location, Plan:appServicePlanId}" -o json 2>$null | ConvertFrom-Json
                            if ($apiAppInfo) {
                                Write-ColorOutput Cyan "     State: $($apiAppInfo.State)"
                                Write-ColorOutput Cyan "     Location: $($apiAppInfo.Location)"
                            }
                        } catch {
                            # Ignore errors getting details
                        }
                        $appServicesToDelete += $apiAppServiceName
                    }
                    if ($errorMsg -match $adminApiAppServiceName -or $errorMsg -match "dev-euw-app-mystira-admin-api") {
                        Write-ColorOutput Yellow "   • Admin API App Service: $adminApiAppServiceName"
                        # Get App Service details
                        try {
                            $adminAppInfo = az webapp show --name $adminApiAppServiceName --resource-group $RG --query "{State:state, Location:location, Plan:appServicePlanId}" -o json 2>$null | ConvertFrom-Json
                            if ($adminAppInfo) {
                                Write-ColorOutput Cyan "     State: $($adminAppInfo.State)"
                                Write-ColorOutput Cyan "     Location: $($adminAppInfo.Location)"
                            }
                        } catch {
                            # Ignore errors getting details
                        }
                        $appServicesToDelete += $adminApiAppServiceName
                    }
                    Write-Output ""
                    Write-ColorOutput Cyan "   Options:"
                    Write-ColorOutput White "   1. Use existing App Services (skip creation, reference existing)"
                    Write-ColorOutput White "   2. Delete existing App Services and recreate"
                    Write-ColorOutput White "   3. Exit (deployment will fail)"
                    Write-Output ""
                    $response = Read-Host "Choose an option (1/2/3)"
                    
                    if ($response -eq "1") {
                        Write-ColorOutput Green "✅ Will use existing App Services and skip creation."
                        Write-ColorOutput Cyan "   Using App Services in resource group: $RG"
                        $script:skipAppServiceCreation = $true
                        $script:existingAppServiceResourceGroup = $RG
                        $script:appServiceUseAttempted = $true
                        Write-ColorOutput Cyan "   Set script-scoped variables: skipAppServiceCreation=$($script:skipAppServiceCreation), existingAppServiceResourceGroup=$($script:existingAppServiceResourceGroup)"
                        Write-Output ""
                        # Set retry flag - don't break, let the code continue to check other errors
                        # The retry will happen at the end of the error handling block
                        $shouldRetry = $true
                        $retryCount++
                        Write-ColorOutput Yellow "   Debug: Set shouldRetry=$shouldRetry, retryCount=$retryCount"
                        Write-ColorOutput Yellow "   Debug: Will retry with skipAppServiceCreation=$($script:skipAppServiceCreation)"
                        # Don't break - continue to check other error types, but this error is handled
                    } elseif ($response -eq "2") {
                        Write-ColorOutput Yellow "   Deleting existing App Services..."
                        foreach ($appServiceName in $appServicesToDelete) {
                            Write-Host "   Deleting $appServiceName..." -NoNewline
                            try {
                                az webapp delete --name $appServiceName --resource-group $RG --yes 2>$null
                                Write-Host " ✓" -ForegroundColor Green
                            } catch {
                                Write-Host " ✗" -ForegroundColor Red
                            }
                        }
                        Write-ColorOutput Green "✅ Deleted. Retrying deployment..."
                        Write-Output ""
                        $shouldRetry = $true
                        $retryCount++
                    } else {
                        Write-ColorOutput Yellow "⚠️  Exiting. Deployment will fail."
                        Write-ColorOutput Cyan "   You can delete the App Services manually and retry."
                        Write-Output ""
                        $shouldRetry = $false
                        break
                    }
                }
                
                # If we already tried using existing Cosmos DB and it failed again with a Cosmos DB error, don't retry
                # BUT only if it's actually a Cosmos DB error (not App Service or other errors)
                if ($script:cosmosUseAttempted -and ($errorMsg -match "cosmos" -or $errorMsg -match "Cosmos" -or $errorMsg -match "DocumentDB") -and -not ($errorMsg -match "Website" -or $errorMsg -match "App Service" -or $errorMsg -match "appservice" -or $errorMsg -match "Conflict.*Website")) {
                    Write-Output ""
                    Write-ColorOutput Red "❌ Cosmos DB 'use existing' failed. Cannot proceed."
                    Write-Output "Error: $errorMsg"
                    exit 1
                }
                
                # Handle storage account conflict - auto switch to eastus2
                if ($errorMsg -match "StorageAccountAlreadyTaken" -or $errorMsg -match "storage account.*already taken") {
                    Write-Output ""
                    Write-ColorOutput Yellow "⚠️  Storage account name conflict detected!"
                    Write-ColorOutput Cyan "   Automatically switching to eastus2 region..."
                    # Switch to eastus2
                    $LOCATION = "eastus2"
                    $RG = "dev-eus2-rg-mystira-app"
                    $resourcePrefix = "dev-eus2"
                    # Recalculate storage name for new region
                    $storageNameBase = ($resourcePrefix + "-st-mystira") -replace '-', ''
                    if ($storageNameBase.Length -gt 24) {
                        $expectedStorageName = $storageNameBase.Substring(0, 24)
                    } else {
                        $expectedStorageName = $storageNameBase
                    }
                    Write-ColorOutput Green "✅ Switched to region: $LOCATION"
                    Write-ColorOutput Green "   Resource Group: $RG"
                    Write-Output ""
                    # Update params and retry
                    $paramsObj.location = @{ value = $LOCATION }
                    $paramsObj.resourcePrefix = @{ value = $resourcePrefix }
                    $paramsObj.newStorageAccountName = @{ value = $expectedStorageName }
                    $paramsContent.parameters = $paramsObj
                    $jsonContent = $paramsContent | ConvertTo-Json -Depth 10 -Compress:$false
                    [System.IO.File]::WriteAllText($paramsFile, $jsonContent, [System.Text.Encoding]::UTF8)
                    $shouldRetry = $true
                    $retryCount++
                }
                # Handle Communication Services conflict - prompt user
                elseif ($errorMsg -match "NameReservationTaken" -and $errorMsg -match "communication") {
                    Write-Output ""
                    Write-ColorOutput Yellow "⚠️  Communication Service name conflict!"
                    $response = Read-Host "Switch to eastus2 region, or use existing? (switch/use)"
                    if ($response -eq 'switch' -or $response -eq 's') {
                        Write-ColorOutput Cyan "   Switching to eastus2 region..."
                        # Switch to eastus2
                        $LOCATION = "eastus2"
                        $RG = "dev-eus2-rg-mystira-app"
                        $resourcePrefix = "dev-eus2"
                        # Recalculate storage name for new region
                        $storageNameBase = ($resourcePrefix + "-st-mystira") -replace '-', ''
                        if ($storageNameBase.Length -gt 24) {
                            $expectedStorageName = $storageNameBase.Substring(0, 24)
                        } else {
                            $expectedStorageName = $storageNameBase
                        }
                        Write-ColorOutput Green "✅ Switched to region: $LOCATION"
                        Write-ColorOutput Green "   Resource Group: $RG"
                        Write-Output ""
                        # Update params and retry
                        $paramsObj.location = @{ value = $LOCATION }
                        $paramsObj.resourcePrefix = @{ value = $resourcePrefix }
                        $paramsObj.newStorageAccountName = @{ value = $expectedStorageName }
                        $paramsContent.parameters = $paramsObj
                        $jsonContent = $paramsContent | ConvertTo-Json -Depth 10 -Compress:$false
                        [System.IO.File]::WriteAllText($paramsFile, $jsonContent, [System.Text.Encoding]::UTF8)
                        $shouldRetry = $true
                        $retryCount++
                    } elseif ($response -eq 'use' -or $response -eq 'u') {
                        Write-ColorOutput Green "✅ Will use existing Communication Service and skip creation."
                        $commName = "${resourcePrefix}-acs-mystira"
                        # Use current resource group
                        $commRG = $RG
                        Write-ColorOutput Green "   Using: $commName in $commRG"
                        $paramsObj.skipCommServiceCreation = @{ value = $true }
                        $paramsObj.existingCommServiceResourceGroup = @{ value = $commRG }
                        $paramsObj.existingCommServiceAccountName = @{ value = $commName }
                        # Update params file and retry ONCE - if it fails again, don't retry
                        $paramsContent.parameters = $paramsObj
                        $jsonContent = $paramsContent | ConvertTo-Json -Depth 10 -Compress:$false
                        [System.IO.File]::WriteAllText($paramsFile, $jsonContent, [System.Text.Encoding]::UTF8)
                        Write-ColorOutput Green "✅ Retrying with existing resource (one attempt only)..."
                        Write-Output ""
                        $shouldRetry = $true
                        $retryCount++
                        # Mark that we've tried "use" - if it fails again, don't retry
                        $script:commServiceUseAttempted = $true
                    } else {
                        Write-ColorOutput Red "❌ Invalid response. Cancelling."
                        exit 1
                    }
                }
                # Handle Cosmos DB conflict or region issues - switch ONLY Cosmos DB to eastus2
                elseif (($errorMsg -match "Dns record.*already taken" -or ($errorMsg -match "BadRequest" -and $errorMsg -match "cosmos") -or ($errorMsg -match "ServiceUnavailable" -and $errorMsg -match "cosmos") -or ($errorMsg -match "high demand" -and $errorMsg -match "region") -or ($errorMsg -match "failed provisioning state")) -and -not $script:cosmosUseAttempted) {
                    Write-Output ""
                    Write-ColorOutput Yellow "⚠️  Cosmos DB issue detected (conflict, region unavailable, or failed state)!"
                    
                    # Check if it's a failed provisioning state that needs deletion
                    $shouldSwitchToEastus2 = $true
                    if ($errorMsg -match "failed provisioning state") {
                        $failedCosmosName = "${resourcePrefix}-cosmos-mystira"
                        Write-ColorOutput Yellow "   Detected failed Cosmos DB account: $failedCosmosName"
                        Write-Host "   Deleting failed account..." -NoNewline
                        try {
                            az cosmosdb delete --name $failedCosmosName --resource-group $RG --yes 2>$null
                            Write-Host " ✓" -ForegroundColor Green
                            Write-ColorOutput Green "✅ Deleted failed account."
                        } catch {
                            Write-Host " ✗" -ForegroundColor Red
                            Write-ColorOutput Yellow "   Could not delete."
                        }
                    }
                    
                    # Switch to using existing Cosmos DB in eastus2 (don't retry creating in current region)
                    Write-ColorOutput Cyan "   Switching Cosmos DB to eastus2 region (keeping other resources in current region)..."
                    # Use existing Cosmos DB in eastus2 - skip creation in current region
                    $cosmosName = "dev-eus2-cosmos-mystira"
                    $cosmosRG = "dev-eus2-rg-mystira-app"
                    Write-ColorOutput Green "   Using Cosmos DB: $cosmosName in $cosmosRG"
                    $script:skipCosmosCreation = $true
                    $paramsObj.skipCosmosCreation = @{ value = $true }
                    $paramsObj.existingCosmosResourceGroup = @{ value = $cosmosRG }
                    $paramsObj.existingCosmosDbAccountName = @{ value = $cosmosName }
                    # Get connection string
                    Write-Host "Getting connection string..." -NoNewline
                    try {
                        $cosmosKeys = az cosmosdb keys list --name $cosmosName --resource-group $cosmosRG --query "primaryMasterKey" -o tsv 2>$null
                        $cosmosEndpoint = az cosmosdb show --name $cosmosName --resource-group $cosmosRG --query "documentEndpoint" -o tsv 2>$null
                        if ($cosmosKeys -and $cosmosEndpoint) {
                            $connString = "AccountEndpoint=$cosmosEndpoint;AccountKey=$cosmosKeys;"
                            $paramsObj.existingCosmosConnectionString = @{ value = $connString }
                            Write-Host " ✓" -ForegroundColor Green
                        } else {
                            Write-Host " (skipped)" -ForegroundColor Yellow
                        }
                    } catch {
                        Write-Host " (skipped)" -ForegroundColor Yellow
                    }
                    # Update params and retry ONCE
                    $paramsContent.parameters = $paramsObj
                    $jsonContent = $paramsContent | ConvertTo-Json -Depth 10 -Compress:$false
                    [System.IO.File]::WriteAllText($paramsFile, $jsonContent, [System.Text.Encoding]::UTF8)
                    Write-ColorOutput Green "✅ Will use existing Cosmos DB in eastus2"
                    Write-ColorOutput Cyan "   Parameters: skipCosmosCreation=$($paramsObj.skipCosmosCreation.value), existingCosmosResourceGroup=$($paramsObj.existingCosmosResourceGroup.value), existingCosmosDbAccountName=$($paramsObj.existingCosmosDbAccountName.value)"
                    Write-Output ""
                    $shouldRetry = $true
                    $retryCount++
                    $script:cosmosUseAttempted = $true
                    Write-ColorOutput Yellow "   (Flag set: cosmosUseAttempted = true - will exit on any Cosmos DB error)"
                }
                # If we already tried using existing Cosmos DB and it failed again, don't retry
                elseif ($script:cosmosUseAttempted -and ($errorMsg -match "cosmos" -or $errorMsg -match "Cosmos" -or $errorMsg -match "DocumentDB")) {
                    Write-Output ""
                    Write-ColorOutput Red "❌ Cosmos DB 'use existing' failed. Cannot proceed."
                    Write-Output "Error: $errorMsg"
                    exit 1
                }
                # Handle storage account conflict - also switch region
                elseif ($errorMsg -match "StorageAccountAlreadyTaken" -or $errorMsg -match "storage account.*already taken") {
                    Write-Output ""
                    Write-ColorOutput Yellow "⚠️  Storage account name conflict detected!"
                    Write-ColorOutput Cyan "   Automatically switching to eastus2 region..."
                    # Switch to eastus2
                    $LOCATION = "eastus2"
                    $RG = "dev-eus2-rg-mystira-app"
                    $resourcePrefix = "dev-eus2"
                    # Recalculate storage name for new region
                    $storageNameBase = ($resourcePrefix + "-st-mystira") -replace '-', ''
                    if ($storageNameBase.Length -gt 24) {
                        $expectedStorageName = $storageNameBase.Substring(0, 24)
                    } else {
                        $expectedStorageName = $storageNameBase
                    }
                    Write-ColorOutput Green "✅ Switched to region: $LOCATION"
                    Write-ColorOutput Green "   Resource Group: $RG"
                    Write-Output ""
                    # Update params and retry
                    $paramsObj.location = @{ value = $LOCATION }
                    $paramsObj.resourcePrefix = @{ value = $resourcePrefix }
                    $paramsObj.newStorageAccountName = @{ value = $expectedStorageName }
                    $paramsContent.parameters = $paramsObj
                    $jsonContent = $paramsContent | ConvertTo-Json -Depth 10 -Compress:$false
                    [System.IO.File]::WriteAllText($paramsFile, $jsonContent, [System.Text.Encoding]::UTF8)
                    $shouldRetry = $true
                    $retryCount++
                }
                else {
                    # Other error - don't retry
                    Write-Output ""
                    Write-ColorOutput Red "❌ Deployment failed!"
                    Write-Output ""
                    Write-Output "Error: $errorMsg"
                    exit 1
                }
            }
        } catch {
            Write-ColorOutput Red "❌ Deployment error: $($_.Exception.Message)"
            exit 1
        } finally {
            # Clean up temp file
            if (Test-Path $paramsFile) {
                Remove-Item $paramsFile -Force
            }
        }
        
        # If deployment succeeded or we shouldn't retry, break the loop
        Write-ColorOutput Yellow "   Debug: After error handling - deploymentSuccess=$deploymentSuccess, shouldRetry=$shouldRetry"
        if ($deploymentSuccess -or -not $shouldRetry) {
            break
        }
        
        # If we've exceeded max retries
        if ($retryCount -gt $maxRetries) {
            Write-ColorOutput Red "❌ Maximum retry attempts exceeded."
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
        
        # Check if Static Web App needs to be created
        if (-not $swaExists) {
            Write-ColorOutput Yellow "📱 Creating Static Web App..."
            Write-Output ""
            
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
                Write-Output ""
                
                # Create Static Web App with GitHub integration
                az staticwebapp create `
                    --name $SWA_NAME `
                    --resource-group $RG `
                    --location $LOCATION `
                    --sku Free `
                    --login-with-github `
                    --repo-url "https://github.com/$repoOwner/$repoName" `
                    --branch "dev" `
                    --app-location "./src/Mystira.App.PWA" `
                    --api-location "./src/Mystira.App.API" `
                    --output-location "build" `
                    --output none 2>$null
                
                Write-ColorOutput Green "✅ Static Web App created!"
                Write-Output ""
                Write-ColorOutput Yellow "⚠️  IMPORTANT: Get the deployment token and add it to GitHub Secrets:"
                Write-Output ""
                Write-Output "   Secret name: AZURE_STATIC_WEB_APPS_API_TOKEN"
                Write-Output ""
                Write-Output "   Get token with:"
                Write-ColorOutput Cyan "   az staticwebapp secrets list --name $SWA_NAME --resource-group $RG --query properties.apiKey -o tsv"
                Write-Output ""
            } else {
                Write-ColorOutput Yellow "⚠️  Could not detect GitHub repository."
                Write-Output "   Please create the Static Web App manually or update the script."
            }
        }
    }
}
