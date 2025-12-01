# Resource Summary Helper Functions
# Provides functions for displaying resource summaries

function Show-ResourceSummary {
    <#
    .SYNOPSIS
    Displays a summary of all resources in a resource group.
    #>
    param(
        [string]$ResourceGroup,
        [string]$Location
    )
    
    Write-Output ""
    Write-ColorOutput Cyan "================================================"
    Write-ColorOutput Cyan "  Resource Summary"
    Write-ColorOutput Cyan "================================================"
    Write-Output ""
    
    # Get all resources in the resource group
    try {
        Write-Output "Resource Group: $ResourceGroup"
        Write-Output "Location: $Location"
        Write-Output ""
        
        $resources = Get-ResourceGroupResources -ResourceGroup $ResourceGroup
        
        # Filter out null/empty resources and ensure we have valid objects
        $validResources = @()
        if ($resources) {
            foreach ($resource in $resources) {
                if ($resource -and $resource.name -and $resource.type) {
                    $validResources += $resource
                }
            }
        }
        
        if ($validResources.Count -gt 0) {
            Write-Output "Resources:"
            
            $appServices = @()
            $storageAccounts = @()
            $cosmosAccounts = @()
            $commServices = @()
            
            foreach ($resource in $validResources | Sort-Object type, name) {
                $resourceType = $resource.type -replace '.*/', ''
                $resourceName = $resource.name
                $resourceState = if ($resource.properties.provisioningState) { $resource.properties.provisioningState } else { "N/A" }
                
                Write-Output "  • $resourceType : $resourceName ($resourceState)"
                
                # Categorize resources for detailed info
                if ($resourceType -eq "sites") {
                    $appServices += $resource
                }
                elseif ($resourceType -eq "storageAccounts") {
                    $storageAccounts += $resource
                }
                elseif ($resourceType -eq "databaseAccounts") {
                    $cosmosAccounts += $resource
                }
                elseif ($resourceType -eq "communicationServices") {
                    $commServices += $resource
                }
            }
            
            # Show detailed info for App Services
            if ($appServices.Count -gt 0) {
                Write-Output ""
                Write-Output "App Services:"
                foreach ($app in $appServices) {
                    $appInfo = Get-AppServiceInfo -Name $app.name -ResourceGroup $ResourceGroup
                    if ($appInfo) {
                        Write-Output "  • $($app.name):"
                        Write-Output "    URL: https://$($appInfo.Url)"
                        Write-Output "    State: $($appInfo.State)"
                        Write-Output "    Location: $($appInfo.Location)"
                    } else {
                        Write-Output "  • $($app.name): (details unavailable)"
                    }
                }
            }
            
            # Show detailed info for Storage Accounts
            if ($storageAccounts.Count -gt 0) {
                Write-Output ""
                Write-Output "Storage Accounts:"
                foreach ($storage in $storageAccounts) {
                    Write-Output "  • $($storage.name)"
                    Write-Output "    Primary Endpoint: https://$($storage.name).blob.core.windows.net"
                }
            }
            
            # Show detailed info for Cosmos DB
            if ($cosmosAccounts.Count -gt 0) {
                Write-Output ""
                Write-Output "Cosmos DB Accounts:"
                foreach ($cosmos in $cosmosAccounts) {
                    $cosmosInfo = Get-CosmosDbInfo -Name $cosmos.name -ResourceGroup $ResourceGroup
                    if ($cosmosInfo) {
                        Write-Output "  • $($cosmos.name):"
                        Write-Output "    Endpoint: $($cosmosInfo.DocumentEndpoint)"
                        Write-Output "    State: $($cosmosInfo.ProvisioningState)"
                    } else {
                        Write-Output "  • $($cosmos.name): (details unavailable)"
                    }
                }
            }
        } else {
            Write-Output "Resources: (none found or unable to retrieve)"
            Write-Log "No valid resources found in resource group $ResourceGroup" "WARN"
        }
        
        # Check for Static Web App
        $swaName = Get-StaticWebAppName $Location
        $swaInfo = Get-StaticWebAppInfo -Name $swaName -ResourceGroup $ResourceGroup
        
        if ($swaInfo) {
            Write-Output ""
            Write-Output "Static Web App:"
            Write-Output "  • Name: $($swaInfo.name)"
            Write-Output "  • URL: https://$($swaInfo.defaultHostname)"
            Write-Output "  • Location: $($swaInfo.location)"
        } else {
            Write-Output ""
            Write-ColorOutput Yellow "  WARNING: Static Web App not found: $swaName"
        }
    }
    catch {
        Write-ColorOutput Yellow "  Could not retrieve resource details: $($_.Exception.Message)"
    }
    
    Write-Output ""
    Write-ColorOutput Cyan "================================================"
}

Export-ModuleMember -Function Show-ResourceSummary

