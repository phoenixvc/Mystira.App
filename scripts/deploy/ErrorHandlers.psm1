# Error Handler Functions
# Handles deployment errors and conflicts

function Write-ColorOutput {
    param($ForegroundColor, $Message)
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    Write-Output $Message
    $host.UI.RawUI.ForegroundColor = $fc
}

function Handle-AppServiceConflict {
    <#
    .SYNOPSIS
    Handles App Service conflicts when they already exist.
    #>
    param(
        [string]$ErrorMsg,
        [string]$ResourcePrefix,
        [string]$RG,
        [ref]$Location,
        [ref]$ResourceGroup,
        [ref]$ResourcePrefixRef,
        [ref]$ParamsObj,
        [ref]$ParamsContent,
        [string]$ParamsFile,
        [ref]$ShouldRetry,
        [ref]$RetryCount,
        [ref]$SkipAppServiceCreation,
        [ref]$ExistingAppServiceResourceGroup,
        [ref]$AppServiceUseAttempted,
        [switch]$Verbose,
        [switch]$WhatIf
    )
    
    if ($WhatIf) {
        Write-ColorOutput Yellow "   [WHATIF] Would handle App Service conflict"
        return
    }
    
    Write-Output ""
    Write-ColorOutput Yellow "WARNING: App Service conflict detected!"
    Write-Output ""
    Write-ColorOutput Cyan "   The following App Services already exist:"
    $appServicesToDelete = @()
    $apiAppServiceName = "${ResourcePrefix}-app-mystira-api"
    $adminApiAppServiceName = "${ResourcePrefix}-app-mystira-admin-api"
    $actualResourceGroup = $RG
    
    if ($ErrorMsg -match $apiAppServiceName -or $ErrorMsg -match "dev-euw-app-mystira-api") {
        Write-ColorOutput Yellow "   • API App Service: $apiAppServiceName"
        try {
            $apiAppList = az webapp list --query "[?name=='$apiAppServiceName'].{ResourceGroup:resourceGroup, State:state, Location:location}" -o json 2>$null | ConvertFrom-Json
            if ($apiAppList -and $apiAppList.Count -gt 0) {
                $actualResourceGroup = $apiAppList[0].ResourceGroup
                Write-ColorOutput Cyan "     Found in resource group: $actualResourceGroup"
                Write-ColorOutput Cyan "     State: $($apiAppList[0].State)"
                Write-ColorOutput Cyan "     Location: $($apiAppList[0].Location)"
            }
        } catch {
            # Ignore errors
        }
        $appServicesToDelete += $apiAppServiceName
    }
    
    if ($ErrorMsg -match $adminApiAppServiceName -or $ErrorMsg -match "dev-euw-app-mystira-admin-api") {
        Write-ColorOutput Yellow "   • Admin API App Service: $adminApiAppServiceName"
        try {
            $adminAppList = az webapp list --query "[?name=='$adminApiAppServiceName'].{ResourceGroup:resourceGroup, State:state, Location:location}" -o json 2>$null | ConvertFrom-Json
            if ($adminAppList -and $adminAppList.Count -gt 0) {
                $actualResourceGroup = $adminAppList[0].ResourceGroup
                Write-ColorOutput Cyan "     Found in resource group: $actualResourceGroup"
                Write-ColorOutput Cyan "     State: $($adminAppList[0].State)"
                Write-ColorOutput Cyan "     Location: $($adminAppList[0].Location)"
            }
        } catch {
            # Ignore errors
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
        Write-ColorOutput Green "SUCCESS: Will use existing App Services and skip creation."
        Write-ColorOutput Cyan "   Using App Services in resource group: $actualResourceGroup"
        $SkipAppServiceCreation.Value = $true
        $ExistingAppServiceResourceGroup.Value = $actualResourceGroup
        $AppServiceUseAttempted.Value = $true
        $ParamsObj.Value.skipAppServiceCreation = @{ value = $true }
        $ParamsObj.Value.existingAppServiceResourceGroup = @{ value = $actualResourceGroup }
        $ShouldRetry.Value = $true
        $RetryCount.Value++
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
        Write-ColorOutput Green "SUCCESS: Deleted. Retrying deployment..."
        $ShouldRetry.Value = $true
        $RetryCount.Value++
    } else {
        Write-ColorOutput Yellow "WARNING: Exiting. Deployment will fail."
        $ShouldRetry.Value = $false
    }
}

function Handle-StorageConflict {
    <#
    .SYNOPSIS
    Handles storage account name conflicts by switching region.
    #>
    param(
        [ref]$Location,
        [ref]$ResourceGroup,
        [ref]$ResourcePrefix,
        [ref]$ExpectedStorageName,
        [ref]$ParamsObj,
        [ref]$ParamsContent,
        [string]$ParamsFile,
        [ref]$ShouldRetry,
        [ref]$RetryCount,
        [switch]$WhatIf
    )
    
    if ($WhatIf) {
        Write-ColorOutput Yellow "   [WHATIF] Would switch to eastus2 region for storage"
        return
    }
    
    Write-Output ""
    Write-ColorOutput Yellow "WARNING: Storage account name conflict detected!"
    Write-ColorOutput Cyan "   Automatically switching to eastus2 region..."
    
    $Location.Value = "eastus2"
    $ResourceGroup.Value = "dev-eus2-rg-mystira-app"
    $ResourcePrefix.Value = "dev-eus2"
    
    $storageNameBase = ($ResourcePrefix.Value + "-st-mystira") -replace '-', ''
    if ($storageNameBase.Length -gt 24) {
        $ExpectedStorageName.Value = $storageNameBase.Substring(0, 24)
    } else {
        $ExpectedStorageName.Value = $storageNameBase
    }
    
    Write-ColorOutput Green "SUCCESS: Switched to region: $($Location.Value)"
    Write-ColorOutput Green "   Resource Group: $($ResourceGroup.Value)"
    
    $ParamsObj.Value.location = @{ value = $Location.Value }
    $ParamsObj.Value.resourcePrefix = @{ value = $ResourcePrefix.Value }
    $ParamsObj.Value.newStorageAccountName = @{ value = $ExpectedStorageName.Value }
    $ParamsContent.Value.parameters = $ParamsObj.Value
    $jsonContent = $ParamsContent.Value | ConvertTo-Json -Depth 10 -Compress:$false
    [System.IO.File]::WriteAllText($ParamsFile, $jsonContent, [System.Text.Encoding]::UTF8)
    
    $ShouldRetry.Value = $true
    $RetryCount.Value++
}

function Handle-CommunicationServiceConflict {
    <#
    .SYNOPSIS
    Handles Communication Service name conflicts.
    #>
    param(
        [string]$ResourcePrefix,
        [string]$RG,
        [ref]$Location,
        [ref]$ResourceGroup,
        [ref]$ResourcePrefixRef,
        [ref]$ExpectedStorageName,
        [ref]$ParamsObj,
        [ref]$ParamsContent,
        [string]$ParamsFile,
        [ref]$ShouldRetry,
        [ref]$RetryCount,
        [ref]$CommServiceUseAttempted,
        [switch]$Verbose,
        [switch]$WhatIf
    )
    
    if ($WhatIf) {
        Write-ColorOutput Yellow "   [WHATIF] Would handle Communication Service conflict"
        return
    }
    
    Write-Output ""
    Write-ColorOutput Yellow "WARNING: Communication Service name conflict!"
    $response = Read-Host "Switch to eastus2 region, or use existing? (switch/use)"
    
    if ($response -eq 'switch' -or $response -eq 's') {
        Write-ColorOutput Cyan "   Switching to eastus2 region..."
        $Location.Value = "eastus2"
        $ResourceGroup.Value = "dev-eus2-rg-mystira-app"
        $ResourcePrefixRef.Value = "dev-eus2"
        
        $storageNameBase = ($ResourcePrefixRef.Value + "-st-mystira") -replace '-', ''
        if ($storageNameBase.Length -gt 24) {
            $ExpectedStorageName.Value = $storageNameBase.Substring(0, 24)
        } else {
            $ExpectedStorageName.Value = $storageNameBase
        }
        
        Write-ColorOutput Green "SUCCESS: Switched to region: $($Location.Value)"
        Write-ColorOutput Green "   Resource Group: $($ResourceGroup.Value)"
        
        $ParamsObj.Value.location = @{ value = $Location.Value }
        $ParamsObj.Value.resourcePrefix = @{ value = $ResourcePrefixRef.Value }
        $ParamsObj.Value.newStorageAccountName = @{ value = $ExpectedStorageName.Value }
        $ParamsContent.Value.parameters = $ParamsObj.Value
        $jsonContent = $ParamsContent.Value | ConvertTo-Json -Depth 10 -Compress:$false
        [System.IO.File]::WriteAllText($ParamsFile, $jsonContent, [System.Text.Encoding]::UTF8)
        
        $ShouldRetry.Value = $true
        $RetryCount.Value++
    } elseif ($response -eq 'use' -or $response -eq 'u') {
        Write-ColorOutput Green "SUCCESS: Will use existing Communication Service and skip creation."
        $commName = "${ResourcePrefix}-acs-mystira"
        $commRG = $RG
        Write-ColorOutput Green "   Using: $commName in $commRG"
        
        $ParamsObj.Value.skipCommServiceCreation = @{ value = $true }
        $ParamsObj.Value.existingCommServiceResourceGroup = @{ value = $commRG }
        $ParamsObj.Value.existingCommServiceAccountName = @{ value = $commName }
        
        $ParamsContent.Value.parameters = $ParamsObj.Value
        $jsonContent = $ParamsContent.Value | ConvertTo-Json -Depth 10 -Compress:$false
        [System.IO.File]::WriteAllText($ParamsFile, $jsonContent, [System.Text.Encoding]::UTF8)
        
        Write-ColorOutput Green "✅ Retrying with existing resource (one attempt only)..."
        $ShouldRetry.Value = $true
        $RetryCount.Value++
        $CommServiceUseAttempted.Value = $true
    } else {
        Write-ColorOutput Red "❌ Invalid response. Cancelling."
        exit 1
    }
}

function Handle-CosmosDbConflict {
    <#
    .SYNOPSIS
    Handles Cosmos DB conflicts and region issues.
    #>
    param(
        [string]$ErrorMsg,
        [string]$ResourcePrefix,
        [string]$RG,
        [ref]$ParamsObj,
        [ref]$ParamsContent,
        [string]$ParamsFile,
        [ref]$ShouldRetry,
        [ref]$RetryCount,
        [ref]$SkipCosmosCreation,
        [ref]$CosmosUseAttempted,
        [switch]$Verbose,
        [switch]$WhatIf
    )
    
    if ($WhatIf) {
        Write-ColorOutput Yellow "   [WHATIF] Would switch Cosmos DB to eastus2"
        return
    }
    
    Write-Output ""
    Write-ColorOutput Yellow "WARNING: Cosmos DB issue detected (conflict, region unavailable, or failed state)!"
    
    if ($ErrorMsg -match "failed provisioning state") {
        $failedCosmosName = "${ResourcePrefix}-cosmos-mystira"
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
    
    Write-ColorOutput Cyan "   Switching Cosmos DB to eastus2 region (keeping other resources in current region)..."
    $cosmosName = "dev-eus2-cosmos-mystira"
    $cosmosRG = "dev-eus2-rg-mystira-app"
    Write-ColorOutput Green "   Using Cosmos DB: $cosmosName in $cosmosRG"
    
    $SkipCosmosCreation.Value = $true
    $ParamsObj.Value.skipCosmosCreation = @{ value = $true }
    $ParamsObj.Value.existingCosmosResourceGroup = @{ value = $cosmosRG }
    $ParamsObj.Value.existingCosmosDbAccountName = @{ value = $cosmosName }
    
    Write-Host "Getting connection string..." -NoNewline
    try {
        $cosmosKeys = az cosmosdb keys list --name $cosmosName --resource-group $cosmosRG --query "primaryMasterKey" -o tsv 2>$null
        $cosmosEndpoint = az cosmosdb show --name $cosmosName --resource-group $cosmosRG --query "documentEndpoint" -o tsv 2>$null
        if ($cosmosKeys -and $cosmosEndpoint) {
            $connString = "AccountEndpoint=$cosmosEndpoint;AccountKey=$cosmosKeys;"
            $ParamsObj.Value.existingCosmosConnectionString = @{ value = $connString }
            Write-Host " ✓" -ForegroundColor Green
        } else {
            Write-Host " (skipped)" -ForegroundColor Yellow
        }
    } catch {
        Write-Host " (skipped)" -ForegroundColor Yellow
    }
    
    $ParamsContent.Value.parameters = $ParamsObj.Value
    $jsonContent = $ParamsContent.Value | ConvertTo-Json -Depth 10 -Compress:$false
    [System.IO.File]::WriteAllText($ParamsFile, $jsonContent, [System.Text.Encoding]::UTF8)
    
    Write-ColorOutput Green "SUCCESS: Will use existing Cosmos DB in eastus2"
    $ShouldRetry.Value = $true
    $RetryCount.Value++
    $CosmosUseAttempted.Value = $true
}

Export-ModuleMember -Function Handle-AppServiceConflict, Handle-StorageConflict, Handle-CommunicationServiceConflict, Handle-CosmosDbConflict, Write-ColorOutput

