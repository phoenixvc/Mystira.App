# Resource Selection Helper Functions
# Provides functions for interactive resource selection

function Show-ResourceSelectionMenu {
    <#
    .SYNOPSIS
    Shows existing resources and prompts user for selection.
    #>
    param(
        [object]$ExistingResources,
        [string]$DefaultResourceGroup,
        [string]$DefaultLocation,
        [string[]]$AvailableRegions
    )
    
    $selection = @{
        ResourceGroup    = $DefaultResourceGroup
        Location         = $DefaultLocation
        StaticWebAppName = Get-StaticWebAppName $DefaultLocation
        Action           = "use_default"
    }
    
    if ($ExistingResources.ResourceGroups.Count -eq 0 -and $ExistingResources.StaticWebApps.Count -eq 0) {
        return $selection
    }
    
    Write-Output ""
    Write-ColorOutput Cyan "Found Existing Resources:"
    Write-Output ""
    
    if ($ExistingResources.ResourceGroups.Count -gt 0) {
        Write-Output "Resource Groups:"
        for ($i = 0; $i -lt $ExistingResources.ResourceGroups.Count; $i++) {
            $rgItem = $ExistingResources.ResourceGroups[$i]
            $status = if ($rgItem.HasResources) { "[OK] Has resources ($($rgItem.ResourceCount))" } else { "Empty" }
            Write-Output "  [$($i+1)] $($rgItem.Name) - $($rgItem.Location) ($status)"
        }
        Write-Output ""
    }
    
    if ($ExistingResources.StaticWebApps.Count -gt 0) {
        Write-Output "Static Web Apps:"
        for ($i = 0; $i -lt $ExistingResources.StaticWebApps.Count; $i++) {
            $swaItem = $ExistingResources.StaticWebApps[$i]
            Write-Output "  [$($i+1)] $($swaItem.Name) - $($swaItem.ResourceGroup) ($($swaItem.Location))"
            Write-Output "      URL: https://$($swaItem.DefaultHostname)"
        }
        Write-Output ""
    }
    
    Write-ColorOutput Yellow "What would you like to do?"
    $maxOption = 4
    $options = @("1. Use existing resource group: $DefaultResourceGroup (in $DefaultLocation)")
    if ($ExistingResources.ResourceGroups.Count -gt 0) {
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
            Write-ColorOutput Red "ERROR: Invalid choice. Please enter a number between 1 and $maxOption."
        }
    } while ($choice -notmatch "^[1-$maxOption]$")
    
    switch ($choice) {
        "1" {
            # Use default - continue as normal
            Write-ColorOutput Green "SUCCESS: Using existing resource group: $DefaultResourceGroup"
            $selection.Action = "use_default"
        }
        "2" {
            if ($ExistingResources.ResourceGroups.Count -gt 0) {
                $rgSelection = Select-ResourceGroupFromList -ResourceGroups $ExistingResources.ResourceGroups
                if ($rgSelection) {
                    $selection.ResourceGroup = $rgSelection.Name
                    $selection.Location = $rgSelection.Location
                    $selection.StaticWebAppName = Get-StaticWebAppName $rgSelection.Location
                    $selection.Action = "use_selected"
                    Write-ColorOutput Green "SUCCESS: Selected: $($rgSelection.Name) in $($rgSelection.Location)"
                }
            }
        }
        "3" {
            $newName = Read-Host "Enter new resource group name (e.g., dev-custom-rg-mystira-app)"
            $selection.ResourceGroup = $newName
            $selection.Action = "create_new"
            Write-ColorOutput Green "SUCCESS: Will create new resource group: $newName"
        }
        "4" {
            $regionSelection = Select-RegionFromList -AvailableRegions $AvailableRegions
            if ($regionSelection) {
                $selection.Location = $regionSelection
                $selection.ResourceGroup = Get-ResourceGroupName $regionSelection
                $selection.StaticWebAppName = Get-StaticWebAppName $regionSelection
                $selection.Action = "create_in_region"
                Write-ColorOutput Green "SUCCESS: Will create in region: $regionSelection (Resource Group: $($selection.ResourceGroup))"
            }
        }
        default {
            Write-ColorOutput Red "ERROR: Invalid choice. Using default: $DefaultResourceGroup"
            $selection.Action = "use_default"
        }
    }
    
    return $selection
}

function Select-ResourceGroupFromList {
    <#
    .SYNOPSIS
    Prompts user to select a resource group from a list.
    #>
    param(
        [array]$ResourceGroups
    )
    
    Write-Output ""
    Write-Output "Select a resource group:"
    for ($i = 0; $i -lt $ResourceGroups.Count; $i++) {
        $rgItem = $ResourceGroups[$i]
        Write-Output "  [$($i+1)] $($rgItem.Name) - $($rgItem.Location)"
    }
    Write-Output ""
    
    # Input validation with retry
    do {
        $rgChoice = Read-Host "Enter number (1-$($ResourceGroups.Count))"
        if ($rgChoice -notmatch "^\d+$" -or [int]$rgChoice -lt 1 -or [int]$rgChoice -gt $ResourceGroups.Count) {
            Write-ColorOutput Red "ERROR: Invalid choice. Please enter a number between 1 and $($ResourceGroups.Count)."
        }
    } while ($rgChoice -notmatch "^\d+$" -or [int]$rgChoice -lt 1 -or [int]$rgChoice -gt $ResourceGroups.Count)
    
    return $ResourceGroups[[int]$rgChoice - 1]
}

function Select-RegionFromList {
    <#
    .SYNOPSIS
    Prompts user to select a region from a list.
    #>
    param(
        [string[]]$AvailableRegions
    )
    
    Write-Output ""
    Write-Output "Available regions:"
    for ($i = 0; $i -lt $AvailableRegions.Count; $i++) {
        Write-Output "  [$($i+1)] $($AvailableRegions[$i])"
    }
    Write-Output ""
    
    # Input validation with retry
    do {
        $regionChoice = Read-Host "Enter region number (1-$($AvailableRegions.Count))"
        if ($regionChoice -notmatch "^\d+$" -or [int]$regionChoice -lt 1 -or [int]$regionChoice -gt $AvailableRegions.Count) {
            Write-ColorOutput Red "ERROR: Invalid choice. Please enter a number between 1 and $($AvailableRegions.Count)."
        }
    } while ($regionChoice -notmatch "^\d+$" -or [int]$regionChoice -lt 1 -or [int]$regionChoice -gt $AvailableRegions.Count)
    
    $selectedRegion = $AvailableRegions[[int]$regionChoice - 1]
    if (-not (Test-Region -Region $selectedRegion -SupportedRegions $AvailableRegions)) {
        Write-ColorOutput Red "ERROR: Invalid region selected"
        return $null
    }
    
    return $selectedRegion
}

Export-ModuleMember -Function Show-ResourceSelectionMenu, Select-ResourceGroupFromList, Select-RegionFromList

