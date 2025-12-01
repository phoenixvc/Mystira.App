# Deploy Config Script - Lightweight configuration updates
# Updates Azure App Service settings without full infrastructure deployment
# Usage: .\.deploy-config.ps1 [-Region southafricanorth|westeurope] [-Setting cors|api|swa|all] [-WhatIf]

param(
    [string]$Region = "southafricanorth",
    [ValidateSet("cors", "api", "swa", "all")]
    [string]$Setting = "cors",
    [switch]$WhatIf,
    [string]$SubscriptionId = "22f9eb18-6553-4b7d-9451-47d0195085fe"
)

# Color output helper
function Write-ColorOutput($ForegroundColor, $Message) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $host.UI.RawUI.ForegroundColor = $fc
}

# Get resource names based on region
function Get-ResourcePrefix($Location) {
    switch ($Location) {
        "southafricanorth" { return "dev-san" }
        "westeurope" { return "dev-euw" }
        default { return "dev-san" }
    }
}

Write-ColorOutput Cyan "🔧 Deploy Config - Quick Configuration Update"
Write-Output "=============================================="
Write-Output ""

# Check Azure login
Write-Host "Checking Azure login... " -NoNewline
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "✗" -ForegroundColor Red
    Write-Output "Logging in..."
    az login --use-device-code
    $account = az account show 2>$null | ConvertFrom-Json
}
Write-Host "✓" -ForegroundColor Green

# Set subscription
az account set --subscription $SubscriptionId 2>$null

$prefix = Get-ResourcePrefix -Location $Region
$RG = "$prefix-rg-mystira-app"
$API_NAME = "$prefix-app-mystira-api"
$ADMIN_API_NAME = "$prefix-app-mystira-admin-api"
$SWA_NAME = "$prefix-swa-mystira-app"

Write-Output ""
Write-Output "Region: $Region"
Write-Output "Resource Group: $RG"
Write-Output "API App Service: $API_NAME"
Write-Output "Static Web App: $SWA_NAME"
Write-Output ""

# CORS origins - keep in sync with infrastructure/dev/main.bicep
$CORS_ORIGINS = @(
    "http://localhost:7000",
    "https://localhost:7000",
    "https://mystira.app",
    "https://blue-water-0eab7991e.3.azurestaticapps.net",
    "https://brave-meadow-0ecd87c03.3.azurestaticapps.net"
) -join ","

if ($Setting -eq "cors" -or $Setting -eq "all") {
    Write-ColorOutput Cyan "📝 Updating CORS settings..."
    Write-Output "   Origins: $CORS_ORIGINS"
    Write-Output ""

    if ($WhatIf) {
        Write-ColorOutput Yellow "[WhatIf] Would update CorsSettings__AllowedOrigins on $API_NAME"
        Write-ColorOutput Yellow "[WhatIf] Would update CorsSettings__AllowedOrigins on $ADMIN_API_NAME"
    } else {
        # Update API
        Write-Host "Updating $API_NAME... " -NoNewline
        $result = az webapp config appsettings set `
            --name $API_NAME `
            --resource-group $RG `
            --settings "CorsSettings__AllowedOrigins=$CORS_ORIGINS" `
            --output none 2>&1

        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓" -ForegroundColor Green
        } else {
            Write-Host "✗" -ForegroundColor Red
            Write-Output "   Error: $result"
        }

        # Update Admin API
        Write-Host "Updating $ADMIN_API_NAME... " -NoNewline
        $result = az webapp config appsettings set `
            --name $ADMIN_API_NAME `
            --resource-group $RG `
            --settings "CorsSettings__AllowedOrigins=$CORS_ORIGINS" `
            --output none 2>&1

        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓" -ForegroundColor Green
        } else {
            Write-Host "✗" -ForegroundColor Red
            Write-Output "   Error: $result"
        }
    }
}

if ($Setting -eq "api" -or $Setting -eq "all") {
    Write-ColorOutput Cyan "🔄 Restarting API services..."

    if ($WhatIf) {
        Write-ColorOutput Yellow "[WhatIf] Would restart $API_NAME"
        Write-ColorOutput Yellow "[WhatIf] Would restart $ADMIN_API_NAME"
    } else {
        Write-Host "Restarting $API_NAME... " -NoNewline
        az webapp restart --name $API_NAME --resource-group $RG --output none 2>&1
        if ($LASTEXITCODE -eq 0) { Write-Host "✓" -ForegroundColor Green } else { Write-Host "✗" -ForegroundColor Red }

        Write-Host "Restarting $ADMIN_API_NAME... " -NoNewline
        az webapp restart --name $ADMIN_API_NAME --resource-group $RG --output none 2>&1
        if ($LASTEXITCODE -eq 0) { Write-Host "✓" -ForegroundColor Green } else { Write-Host "✗" -ForegroundColor Red }
    }
}

if ($Setting -eq "swa" -or $Setting -eq "all") {
    Write-ColorOutput Cyan "🔌 Disconnecting Static Web App built-in CI/CD..."
    Write-Output "   This allows GitHub Actions to take over deployments"
    Write-Output ""

    if ($WhatIf) {
        Write-ColorOutput Yellow "[WhatIf] Would disconnect $SWA_NAME from built-in CI/CD"
    } else {
        Write-Host "Disconnecting $SWA_NAME... " -NoNewline
        $result = az staticwebapp disconnect --name $SWA_NAME --resource-group $RG 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓" -ForegroundColor Green
            Write-Output ""
            Write-ColorOutput Yellow "   Next steps:"
            Write-Output "   1. Get deployment token:"
            Write-ColorOutput Cyan "      az staticwebapp secrets list --name $SWA_NAME --resource-group $RG --query properties.apiKey -o tsv"
            Write-Output "   2. Add to GitHub Secrets as: AZURE_STATIC_WEB_APPS_API_TOKEN_DEV_SAN_MYSTIRA_APP"
        } else {
            Write-Host "✗" -ForegroundColor Red
            Write-Output "   Error: $result"
            Write-Output "   (This may fail if already disconnected or not connected)"
        }
    }
}

Write-Output ""
Write-ColorOutput Green "✅ Configuration update complete!"
Write-Output ""
Write-Output "To verify CORS settings:"
Write-ColorOutput Cyan "   az webapp config appsettings list --name $API_NAME --resource-group $RG --query `"[?name=='CorsSettings__AllowedOrigins']`""
