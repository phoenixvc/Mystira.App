# Infrastructure Detection Helper Functions
# Provides functions for detecting existing Azure infrastructure

function Get-InfrastructureStatus {
    <#
    .SYNOPSIS
    Detects the current infrastructure status for a given location.
    #>
    param(
        [string]$Location,
        [string]$ResourceGroup,
        [string]$StaticWebAppName,
        [object]$ExistingResources,
        [switch]$Verbose
    )
    
    $status = @{
        ResourceGroupExists    = $false
        HasResources           = $false
        StaticWebAppExists     = $false
        InfrastructureDeployed = $false
    }
    
    # Check resource group directly (more reliable than scan)
    Write-Log "Checking resource group $ResourceGroup directly..." "INFO"
    try {
        $rgInfo = Get-ResourceGroupInfo -Name $ResourceGroup
        if ($rgInfo) {
            $status.ResourceGroupExists = $true
            Write-Log "Resource group $ResourceGroup exists" "INFO"
            
            # Check for resources in the group
            $resources = Get-ResourceGroupResources -ResourceGroup $ResourceGroup
            if ($resources -and $resources.Count -gt 0) {
                $status.HasResources = $true
                Write-Log "Found $($resources.Count) resources in resource group $ResourceGroup" "INFO"
            }
            else {
                Write-Log "Resource group $ResourceGroup exists but has no resources" "INFO"
            }
        }
    }
    catch {
        Write-Log "Resource group $ResourceGroup does not exist or check failed: $($_.Exception.Message)" "INFO"
    }
    
    # Also check scan results as backup
    if ($ExistingResources) {
        $matchingRG = $ExistingResources.ResourceGroups | Where-Object { $_.Name -eq $ResourceGroup }
        if ($matchingRG -and -not $status.ResourceGroupExists) {
            $status.ResourceGroupExists = $true
            $status.HasResources = $matchingRG.HasResources
            Write-Log "Resource group $ResourceGroup found via scan" "INFO"
        }
        elseif ($matchingRG -and $matchingRG.HasResources -and -not $status.HasResources) {
            # If scan says it has resources but direct check didn't find them, trust the scan
            $status.HasResources = $true
            Write-Log "Using scan result: resource group $ResourceGroup has resources" "INFO"
        }
    }
    
    # Check if Static Web App exists
    if ($ExistingResources) {
        $matchingSWA = $ExistingResources.StaticWebApps | Where-Object { $_.Name -eq $StaticWebAppName -and $_.ResourceGroup -eq $ResourceGroup }
        if ($matchingSWA) {
            $status.StaticWebAppExists = $true
            Write-Log "Static Web App $StaticWebAppName found via scan" "INFO"
        }
    }
    
    # Also check directly (with proper validation)
    if (-not $status.StaticWebAppExists) {
        try {
            $swaInfo = Get-StaticWebAppInfo -Name $StaticWebAppName -ResourceGroup $ResourceGroup
            # Validate it's actually a SWA (has name property and no error)
            if ($swaInfo -and $swaInfo.name -and -not $swaInfo.error) {
                $status.StaticWebAppExists = $true
                Write-Log "Static Web App $StaticWebAppName found via direct check" "INFO"
            }
            else {
                Write-Log "Static Web App $StaticWebAppName does not exist (invalid response)" "INFO"
            }
        }
        catch {
            # SWA doesn't exist
            Write-Log "Static Web App $StaticWebAppName does not exist" "INFO"
        }
    }
    
    # Infrastructure is deployed if either has resources in RG or SWA exists
    $status.InfrastructureDeployed = ($status.HasResources -or $status.StaticWebAppExists)
    
    return $status
}

function Test-ResourceExists {
    <#
    .SYNOPSIS
    Tests if a specific Azure resource exists with proper validation.
    #>
    param(
        [string]$ResourceType,
        [string]$Name,
        [string]$ResourceGroup
    )
    
    try {
        switch ($ResourceType) {
            "Microsoft.Web/staticSites" {
                return Test-StaticWebAppExists -Name $Name -ResourceGroup $ResourceGroup
            }
            "Microsoft.Web/sites" {
                $info = Get-AppServiceInfo -Name $Name -ResourceGroup $ResourceGroup
                return ($null -ne $info -and $info.name -and -not $info.error)
            }
            "Microsoft.DocumentDB/databaseAccounts" {
                $info = Get-CosmosDbInfo -Name $Name -ResourceGroup $ResourceGroup
                return ($null -ne $info -and $info.name -and -not $info.error)
            }
            "Microsoft.Storage/storageAccounts" {
                $info = Get-StorageAccountInfo -Name $Name -ResourceGroup $ResourceGroup
                return ($null -ne $info -and $info.name -and -not $info.error)
            }
            default {
                # Generic check using az resource show
                $result = az resource show --name $Name --resource-group $ResourceGroup --resource-type $ResourceType --output json 2>$null
                if ($LASTEXITCODE -eq 0 -and $result) {
                    $resource = $result | ConvertFrom-Json -ErrorAction SilentlyContinue
                    return ($null -ne $resource -and $resource.name -and -not $resource.error)
                }
                return $false
            }
        }
    }
    catch {
        return $false
    }
    
    return $false
}

Export-ModuleMember -Function Get-InfrastructureStatus, Test-ResourceExists

