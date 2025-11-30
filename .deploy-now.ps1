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
        "eastus2"     { return "dev-eus2-rg-mystira-app" }
        "westus2"     { return "dev-wus2-rg-mystira-app" }
        "centralus"   { return "dev-cus-rg-mystira-app" }
        "westeurope"  { return "dev-euw-rg-mystira-app" }
        "northeurope" { return "dev-eun-rg-mystira-app" }
        "eastasia"    { return "dev-ea-rg-mystira-app" }
        default       { return "dev-$($loc.Substring(0,4))-rg-mystira-app" }
    }
}

$RG = Get-ResourceGroupName $LOCATION

Write-ColorOutput Yellow "🚀 Deploy Now - Smart Deployment"
Write-ColorOutput Yellow "================================="
Write-Output ""

# Check if Azure CLI is available
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-ColorOutput Red "❌ Azure CLI not installed."
    Write-Output "Install: https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
}

# Check Azure login
Write-Output "Checking Azure login... " -NoNewline
try {
    $account = az account show 2>$null | ConvertFrom-Json
    if ($account) {
        Write-ColorOutput Green "OK ($($account.name))"
    } else {
        throw "Not logged in"
    }
} catch {
    Write-ColorOutput Yellow "not logged in"
    Write-Output "Logging in with device code..."
    az login --use-device-code
}

# Set subscription
Write-Output "Setting subscription... " -NoNewline
try {
    az account set --subscription $SUB 2>$null | Out-Null
    Write-ColorOutput Green "OK"
} catch {
    Write-ColorOutput Red "Failed"
    Write-Output "Available subscriptions:"
    az account list --output table
    exit 1
}

# Check if resource group exists and has resources
Write-Output "Checking infrastructure... " -NoNewline
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
} catch {
    # Resource group doesn't exist
}

if ($hasResources) {
    Write-ColorOutput Green "Resources exist"
    Write-Output ""
    Write-ColorOutput Cyan "✅ Infrastructure is deployed. Pushing code to trigger CI/CD..."
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
        } else {
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
        } else {
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
    
} else {
    Write-ColorOutput Yellow "No resources found"
    Write-Output ""
    Write-ColorOutput Cyan "🏗️  Infrastructure not deployed. Deploying infrastructure..."
    Write-Output ""
    Write-Output "Region: $LOCATION"
    Write-Output "Resource Group: $RG"
    Write-Output ""
    
    # Create resource group if it doesn't exist
    if (-not $rgExists) {
        Write-Output "Creating resource group... " -NoNewline
        az group create --name $RG --location $LOCATION --output none 2>$null
        Write-ColorOutput Green "OK"
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
    $JWT_SECRET = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | ForEach-Object {[char]$_})
    $JWT_SECRET_BASE64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($JWT_SECRET))
    
    # Deploy infrastructure
    Write-Output "Deploying infrastructure (this takes 5-10 minutes)..."
    $deployName = "mystira-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    
    Write-Output ""
    az deployment group create `
        --resource-group $RG `
        --template-file $BICEP_FILE `
        --parameters environment=dev location=$LOCATION jwtSecretKey=$JWT_SECRET_BASE64 `
        --mode Incremental `
        --name $deployName `
        --output table
    
    if ($LASTEXITCODE -eq 0) {
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
        Write-ColorOutput Cyan "✅ Infrastructure ready! You can now deploy code."
    } else {
        Write-ColorOutput Red "❌ Infrastructure deployment failed"
        exit 1
    }
}
