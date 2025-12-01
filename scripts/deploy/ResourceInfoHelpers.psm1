# Resource Info Helper Functions
# Provides functions for retrieving detailed information about Azure resources

function Get-AppServiceInfo {
    <#
    .SYNOPSIS
    Gets detailed information about an App Service.
    #>
    param(
        [string]$Name,
        [string]$ResourceGroup
    )
    
    try {
        if (Get-Command Invoke-AzureCliWithRetry -ErrorAction SilentlyContinue) {
            $result = Invoke-AzureCliWithRetry -Command "az webapp show --name `"$Name`" --resource-group `"$ResourceGroup`" --query `"{Url:defaultHostName, State:state, Location:location}`" -o json" -MaxRetries 2
            if ($result) {
                return $result | ConvertFrom-Json -ErrorAction SilentlyContinue
            }
        }
        else {
            $result = az webapp show --name $Name --resource-group $ResourceGroup --query "{Url:defaultHostName, State:state, Location:location}" -o json 2>$null
            if ($result) {
                return $result | ConvertFrom-Json
            }
        }
    }
    catch {
        Write-Log "Failed to get App Service info: $($_.Exception.Message)" "WARN"
    }
    
    return $null
}

function Get-CosmosDbInfo {
    <#
    .SYNOPSIS
    Gets detailed information about a Cosmos DB account.
    #>
    param(
        [string]$Name,
        [string]$ResourceGroup
    )
    
    try {
        if (Get-Command Invoke-AzureCliWithRetry -ErrorAction SilentlyContinue) {
            $result = Invoke-AzureCliWithRetry -Command "az cosmosdb show --name `"$Name`" --resource-group `"$ResourceGroup`" --query `"{DocumentEndpoint:documentEndpoint, ProvisioningState:provisioningState}`" -o json" -MaxRetries 2
            if ($result) {
                return $result | ConvertFrom-Json -ErrorAction SilentlyContinue
            }
        }
        else {
            $result = az cosmosdb show --name $Name --resource-group $ResourceGroup --query "{DocumentEndpoint:documentEndpoint, ProvisioningState:provisioningState}" -o json 2>$null
            if ($result) {
                return $result | ConvertFrom-Json
            }
        }
    }
    catch {
        Write-Log "Failed to get Cosmos DB info: $($_.Exception.Message)" "WARN"
    }
    
    return $null
}

function Get-StorageAccountInfo {
    <#
    .SYNOPSIS
    Gets detailed information about a Storage Account.
    #>
    param(
        [string]$Name,
        [string]$ResourceGroup
    )
    
    try {
        if (Get-Command Invoke-AzureCliWithRetry -ErrorAction SilentlyContinue) {
            $result = Invoke-AzureCliWithRetry -Command "az storage account show --name `"$Name`" --resource-group `"$ResourceGroup`" --query `"{PrimaryEndpoints:primaryEndpoints, Location:location}`" -o json" -MaxRetries 2
            if ($result) {
                return $result | ConvertFrom-Json -ErrorAction SilentlyContinue
            }
        }
        else {
            $result = az storage account show --name $Name --resource-group $ResourceGroup --query "{PrimaryEndpoints:primaryEndpoints, Location:location}" -o json 2>$null
            if ($result) {
                return $result | ConvertFrom-Json
            }
        }
    }
    catch {
        Write-Log "Failed to get Storage Account info: $($_.Exception.Message)" "WARN"
    }
    
    return $null
}

function Get-StaticWebAppInfo {
    <#
    .SYNOPSIS
    Gets detailed information about a Static Web App.
    #>
    param(
        [string]$Name,
        [string]$ResourceGroup
    )
    
    try {
        # Always use direct az command to properly check exit code
        # Redirect stderr to null to avoid error messages, but capture exit code
        $result = az staticwebapp show --name $Name --resource-group $ResourceGroup --output json 2>$null
        $exitCode = $LASTEXITCODE
        
        # Only process if command succeeded (exit code 0)
        if ($exitCode -eq 0 -and $result) {
            $swaInfo = $result | ConvertFrom-Json -ErrorAction SilentlyContinue
            # Check if it's actually a SWA (not an error response)
            # Valid SWA has a 'name' property and no 'error' property
            if ($swaInfo -and $swaInfo.name -and -not $swaInfo.error) {
                return $swaInfo
            }
        }
    }
    catch {
        Write-Log "Failed to get Static Web App info: $($_.Exception.Message)" "WARN"
    }
    
    return $null
}

function Get-ResourceGroupResources {
    <#
    .SYNOPSIS
    Gets all resources in a resource group.
    #>
    param(
        [string]$ResourceGroup
    )
    
    try {
        if (Get-Command Invoke-AzureCliWithRetry -ErrorAction SilentlyContinue) {
            $result = Invoke-AzureCliWithRetry -Command "az resource list --resource-group `"$ResourceGroup`" --output json" -MaxRetries 2
            if ($result) {
                return $result | ConvertFrom-Json -ErrorAction SilentlyContinue
            }
        }
        else {
            $result = az resource list --resource-group $ResourceGroup --output json 2>$null
            if ($result) {
                return $result | ConvertFrom-Json
            }
        }
    }
    catch {
        Write-Log "Failed to list resources: $($_.Exception.Message)" "WARN"
    }
    
    return @()
}

function Get-ResourcesByType {
    <#
    .SYNOPSIS
    Gets resources of a specific type in a resource group.
    #>
    param(
        [string]$ResourceGroup,
        [string]$ResourceType
    )
    
    try {
        if (Get-Command Invoke-AzureCliWithRetry -ErrorAction SilentlyContinue) {
            $result = Invoke-AzureCliWithRetry -Command "az resource list --resource-group `"$ResourceGroup`" --resource-type `"$ResourceType`" --output json" -MaxRetries 2
            if ($result) {
                return $result | ConvertFrom-Json -ErrorAction SilentlyContinue
            }
        }
        else {
            $result = az resource list --resource-group $ResourceGroup --resource-type $ResourceType --output json 2>$null
            if ($result) {
                return $result | ConvertFrom-Json
            }
        }
    }
    catch {
        Write-Log "Failed to list resources by type: $($_.Exception.Message)" "WARN"
    }
    
    return @()
}

Export-ModuleMember -Function Get-AppServiceInfo, Get-CosmosDbInfo, Get-StorageAccountInfo, Get-StaticWebAppInfo, Get-ResourceGroupResources, Get-ResourcesByType

