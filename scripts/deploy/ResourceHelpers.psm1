# Resource Helper Functions
# Provides resource scanning, validation, and naming functions

function Get-ResourceGroupName {
    <#
    .SYNOPSIS
    Gets the resource group name for a given location.
    Naming convention: [org]-[env]-[project]-rg-[region]
    #>
    param(
        [string]$Location,
        [string]$Environment = "dev",
        [string]$Org = "mys",
        [string]$Project = "mystira"
    )

    $regionCode = switch ($Location) {
        "southafricanorth" { "san" }
        "eastus2" { "eus2" }
        "eastus" { "eus" }
        "westus2" { "usw" }
        "westus" { "wus" }
        "centralus" { "cus" }
        "westeurope" { "euw" }
        "northeurope" { "eun" }
        "eastasia" { "ea" }
        "uksouth" { "uks" }
        "swedencentral" { "swe" }
        default { $Location.Substring(0, [Math]::Min(3, $Location.Length)) }
    }

    return "$Org-$Environment-$Project-rg-$regionCode"
}

function Get-StaticWebAppName {
    <#
    .SYNOPSIS
    Gets the Static Web App name for a given location.
    Naming convention: [org]-[env]-[project]-swa-[region]
    #>
    param(
        [string]$Location,
        [string]$Environment = "dev",
        [string]$Org = "mys",
        [string]$Project = "mystira"
    )

    $regionCode = switch ($Location) {
        "southafricanorth" { "san" }
        "eastus2" { "eus2" }
        "eastus" { "eus" }
        "westus2" { "usw" }
        "westus" { "wus" }
        "centralus" { "cus" }
        "westeurope" { "euw" }
        "northeurope" { "eun" }
        "eastasia" { "ea" }
        "uksouth" { "uks" }
        "swedencentral" { "swe" }
        default { $Location.Substring(0, [Math]::Min(3, $Location.Length)) }
    }

    return "$Org-$Environment-$Project-swa-$regionCode"
}

function Get-ResourcePrefix {
    <#
    .SYNOPSIS
    Gets the resource prefix for a given location.
    Naming convention: [org]-[env]-[project]
    #>
    param(
        [string]$Location,
        [string]$Environment = "dev",
        [string]$Org = "mys",
        [string]$Project = "mystira"
    )

    return "$Org-$Environment-$Project"
}

function Get-ExistingResources {
    <#
    .SYNOPSIS
    Scans for existing Azure resources (resource groups and Static Web Apps).
    
    .PARAMETER SkipScan
    If true, returns empty results without scanning
    
    .PARAMETER Verbose
    Show verbose output
    
    .PARAMETER TimeoutSeconds
    Timeout in seconds for Azure CLI commands
    #>
    param(
        [switch]$SkipScan,
        [switch]$Verbose,
        [int]$TimeoutSeconds = 30
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
    
    Write-Host "[OK]" -ForegroundColor Green
    return @{
        ResourceGroups = $existingRGs
        StaticWebApps  = $existingSWAs
    }
}

function Get-ExpectedStorageAccountName {
    <#
    .SYNOPSIS
    Calculates the expected storage account name.
    Storage account naming: [org][env][project]st[region] (no dashes, max 24 chars)
    #>
    param(
        [string]$Location,
        [string]$Environment = "dev",
        [string]$Org = "mys",
        [string]$Project = "mystira"
    )

    $regionCode = switch ($Location) {
        "southafricanorth" { "san" }
        "eastus2" { "eus2" }
        "eastus" { "eus" }
        "westus2" { "usw" }
        "westus" { "wus" }
        "centralus" { "cus" }
        "westeurope" { "euw" }
        "northeurope" { "eun" }
        "eastasia" { "ea" }
        "uksouth" { "uks" }
        "swedencentral" { "swe" }
        default { $Location.Substring(0, [Math]::Min(3, $Location.Length)) }
    }

    # Storage account: [org][env][project]st[region] (no dashes)
    $storageNameBase = "$Org$Environment${Project}st$regionCode" -replace '-', ''
    if ($storageNameBase.Length -gt 24) {
        return $storageNameBase.Substring(0, 24)
    }
    else {
        return $storageNameBase.ToLower()
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

Export-ModuleMember -Function Get-ResourceGroupName, Get-StaticWebAppName, Get-ResourcePrefix, Get-ExpectedStorageAccountName, Get-ExistingResources, Test-Region

