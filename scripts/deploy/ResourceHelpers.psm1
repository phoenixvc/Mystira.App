# Resource Helper Functions
# Provides resource scanning, validation, and naming functions

function Get-ResourceGroupName {
    <#
    .SYNOPSIS
    Gets the resource group name for a given location.
    #>
    param([string]$Location)
    
    switch ($Location) {
        "southafricanorth" { return "dev-san-rg-mystira-app" }
        "eastus2" { return "dev-eus2-rg-mystira-app" }
        "westus2" { return "dev-wus2-rg-mystira-app" }
        "centralus" { return "dev-cus-rg-mystira-app" }
        "westeurope" { return "dev-euw-rg-mystira-app" }
        "northeurope" { return "dev-eun-rg-mystira-app" }
        "eastasia" { return "dev-ea-rg-mystira-app" }
        default { return "dev-$($Location.Substring(0,4))-rg-mystira-app" }
    }
}

function Get-StaticWebAppName {
    <#
    .SYNOPSIS
    Gets the Static Web App name for a given location.
    #>
    param([string]$Location)
    
    switch ($Location) {
        "southafricanorth" { return "dev-san-swa-mystira-app" }
        "eastus2" { return "dev-eus2-swa-mystira-app" }
        "westus2" { return "dev-wus2-swa-mystira-app" }
        "centralus" { return "dev-cus-swa-mystira-app" }
        "westeurope" { return "dev-euw-swa-mystira-app" }
        "northeurope" { return "dev-eun-swa-mystira-app" }
        "eastasia" { return "dev-ea-swa-mystira-app" }
        default { return "dev-$($Location.Substring(0,4))-swa-mystira-app" }
    }
}

function Get-ResourcePrefix {
    <#
    .SYNOPSIS
    Gets the resource prefix for a given location.
    #>
    param([string]$Location)
    
    switch ($Location) {
        "southafricanorth" { return "dev-san" }
        "eastus2" { return "dev-eus2" }
        "westus2" { return "dev-wus2" }
        "centralus" { return "dev-cus" }
        "westeurope" { return "dev-euw" }
        "northeurope" { return "dev-eun" }
        "eastasia" { return "dev-ea" }
        default { return "dev-$($Location.Substring(0, 4))" }
    }
}

function Get-ExistingResources {
    <#
    .SYNOPSIS
    Scans for existing Azure resources (resource groups and Static Web Apps).
    
    .PARAMETER SkipScan
    If true, returns empty results without scanning
    
    .PARAMETER Verbose
    Show verbose output
    #>
    param(
        [switch]$SkipScan,
        [switch]$Verbose
    )
    
    if ($SkipScan) {
        return @{
            ResourceGroups = @()
            StaticWebApps  = @()
        }
    }
    
    Write-Host "Scanning for existing resources... " -NoNewline
    
    $existingRGs = @()
    $existingSWAs = @()
    
    try {
        # Get all resource groups containing "mystira-app" with timeout
        $rgCmd = "az group list --query `"[?contains(name, 'mystira-app')].{Name:name, Location:location, State:properties.provisioningState}`" -o json"
        $rgListJson = Invoke-AzCliWithTimeout -Command $rgCmd -TimeoutSeconds $TimeoutSeconds -Verbose:$Verbose
        if ($rgListJson) {
            try {
                $rgList = $rgListJson | ConvertFrom-Json -ErrorAction Stop
                if ($rgList) {
                    foreach ($rgItem in $rgList) {
                        # Check if RG has resources with timeout
                        $resCmd = "az resource list --resource-group `"$($rgItem.Name)`" --output json"
                        $resourcesJson = Invoke-AzCliWithTimeout -Command $resCmd -TimeoutSeconds $TimeoutSeconds -Verbose:$Verbose
                        $resources = $null
                        if ($resourcesJson) {
                            try {
                                $resources = $resourcesJson | ConvertFrom-Json -ErrorAction Stop
                            }
                            catch {
                                # Ignore JSON parse errors
                            }
                        }
                        $hasResources = $resources -and $resources.Count -gt 0
                        
                        $existingRGs += @{
                            Name          = $rgItem.Name
                            Location      = $rgItem.Location
                            State         = $rgItem.State
                            HasResources  = $hasResources
                            ResourceCount = if ($resources) { $resources.Count } else { 0 }
                        }
                    }
                }
            }
            catch {
                # Ignore JSON parse errors
                if ($Verbose) {
                    Write-Warning "Failed to parse resource groups JSON: $($_.Exception.Message)"
                }
            }
        }
        
        # Get all Static Web Apps with timeout
        $swaCmd = "az staticwebapp list --query `"[].{Name:name, ResourceGroup:resourceGroup, Location:location, DefaultHostname:defaultHostname}`" -o json"
        $swaListJson = Invoke-AzCliWithTimeout -Command $swaCmd -TimeoutSeconds $TimeoutSeconds -Verbose:$Verbose
        if ($swaListJson) {
            try {
                $swaList = $swaListJson | ConvertFrom-Json -ErrorAction Stop
                if ($swaList) {
                    foreach ($swaItem in $swaList) {
                        $existingSWAs += @{
                            Name            = $swaItem.Name
                            ResourceGroup   = $swaItem.ResourceGroup
                            Location        = $swaItem.Location
                            DefaultHostname = $swaItem.DefaultHostname
                        }
                    }
                }
            }
            catch {
                # Ignore JSON parse errors
                if ($Verbose) {
                    Write-Warning "Failed to parse Static Web Apps JSON: $($_.Exception.Message)"
                }
            }
        }
    }
    catch {
        # Continue even if scanning fails
        if ($Verbose) {
            Write-Warning "Resource scanning error: $($_.Exception.Message)"
        }
    }
    
    Write-Host "✓" -ForegroundColor Green
    return @{
        ResourceGroups = $existingRGs
        StaticWebApps  = $existingSWAs
    }
}

function Test-Region {
    <#
    .SYNOPSIS
    Validates that a region is in the supported list.
    #>
    param(
        [string]$Region,
        [string[]]$SupportedRegions
    )
    
    return $SupportedRegions -contains $Region
}

Export-ModuleMember -Function Get-ResourceGroupName, Get-StaticWebAppName, Get-ResourcePrefix, Get-ExistingResources, Test-Region

