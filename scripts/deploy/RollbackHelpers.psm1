# Rollback Helper Functions
# Tracks created resources and provides rollback functionality

$script:CreatedResources = @{
    ResourceGroup = $null
    Resources = @()
    DeploymentName = $null
    StartTime = $null
}

function Register-Resource {
    <#
    .SYNOPSIS
    Registers a resource that was created during deployment.
    
    .PARAMETER ResourceType
    Type of resource (e.g., "Microsoft.Storage/storageAccounts")
    
    .PARAMETER ResourceName
    Name of the resource
    
    .PARAMETER ResourceGroup
    Resource group containing the resource
    #>
    param(
        [string]$ResourceType,
        [string]$ResourceName,
        [string]$ResourceGroup
    )
    
    $script:CreatedResources.Resources += @{
        Type = $ResourceType
        Name = $ResourceName
        ResourceGroup = $ResourceGroup
        CreatedAt = Get-Date
    }
    
    Write-Log "Registered resource: $ResourceType/$ResourceName in $ResourceGroup" "INFO"
}

function Initialize-RollbackTracking {
    <#
    .SYNOPSIS
    Initializes rollback tracking for a deployment.
    #>
    param(
        [string]$ResourceGroup,
        [string]$DeploymentName
    )
    
    $script:CreatedResources.ResourceGroup = $ResourceGroup
    $script:CreatedResources.DeploymentName = $DeploymentName
    $script:CreatedResources.StartTime = Get-Date
    $script:CreatedResources.Resources = @()
    
    Write-Log "Initialized rollback tracking for deployment: $DeploymentName" "INFO"
}

function Get-CreatedResources {
    <#
    .SYNOPSIS
    Gets the list of created resources.
    #>
    return $script:CreatedResources
}

function Invoke-Rollback {
    <#
    .SYNOPSIS
    Rolls back created resources by deleting them.
    
    .PARAMETER Confirm
    Skip confirmation prompt
    #>
    param(
        [switch]$Confirm
    )
    
    if ($script:CreatedResources.Resources.Count -eq 0) {
        Write-ColorOutput Yellow "No resources to rollback."
        return
    }
    
    Write-Output ""
    Write-ColorOutput Yellow "WARNING: Rollback will delete the following resources:"
    Write-Output ""
    foreach ($resource in $script:CreatedResources.Resources) {
        Write-Output "  • $($resource.Type): $($resource.Name) (in $($resource.ResourceGroup))"
    }
    Write-Output ""
    
    if (-not $Confirm) {
        $response = Read-Host "Do you want to proceed with rollback? (yes/no)"
        if ($response -ne "yes" -and $response -ne "y") {
            Write-ColorOutput Yellow "Rollback cancelled."
            return
        }
    }
    
    Write-ColorOutput Cyan "🔄 Starting rollback..."
    Write-Log "Starting rollback of $($script:CreatedResources.Resources.Count) resources" "INFO"
    
    $failed = @()
    foreach ($resource in $script:CreatedResources.Resources) {
        try {
            Write-Host "Deleting $($resource.Type): $($resource.Name)..." -NoNewline
            $result = az resource delete `
                --ids "/subscriptions/$($script:SUB)/resourceGroups/$($resource.ResourceGroup)/providers/$($resource.Type)/$($resource.Name)" `
                --output none 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host " ✓" -ForegroundColor Green
                Write-Log "Deleted resource: $($resource.Type)/$($resource.Name)" "INFO"
            } else {
                Write-Host " ✗" -ForegroundColor Red
                Write-Log "Failed to delete resource: $($resource.Type)/$($resource.Name) - $result" "ERROR"
                $failed += $resource
            }
        } catch {
            Write-Host " ✗" -ForegroundColor Red
            Write-Log "Exception deleting resource: $($resource.Type)/$($resource.Name) - $($_.Exception.Message)" "ERROR"
            $failed += $resource
        }
    }
    
    # Delete resource group if empty
    if ($script:CreatedResources.ResourceGroup) {
        try {
            Write-Host "Checking if resource group is empty..." -NoNewline
            $resources = az resource list --resource-group $script:CreatedResources.ResourceGroup --output json 2>$null | ConvertFrom-Json
            if (-not $resources -or $resources.Count -eq 0) {
                Write-Host " ✓" -ForegroundColor Green
                Write-Host "Deleting empty resource group: $($script:CreatedResources.ResourceGroup)..." -NoNewline
                az group delete --name $script:CreatedResources.ResourceGroup --yes --output none 2>$null
                if ($LASTEXITCODE -eq 0) {
                    Write-Host " ✓" -ForegroundColor Green
                    Write-Log "Deleted empty resource group: $($script:CreatedResources.ResourceGroup)" "INFO"
                } else {
                    Write-Host " ✗" -ForegroundColor Yellow
                    Write-Log "Could not delete resource group (may not be empty)" "WARN"
                }
            } else {
                Write-Host " (has $($resources.Count) remaining resources)" -ForegroundColor Yellow
            }
        } catch {
            Write-Log "Could not check resource group status" "WARN"
        }
    }
    
    if ($failed.Count -gt 0) {
        Write-Output ""
        Write-ColorOutput Yellow "WARNING: Some resources could not be deleted:"
        foreach ($resource in $failed) {
            Write-Output "  • $($resource.Type): $($resource.Name)"
        }
        Write-Output ""
        Write-ColorOutput Cyan "   You may need to delete these manually from the Azure portal."
    } else {
        Write-Output ""
        Write-ColorOutput Green "SUCCESS: Rollback completed successfully!"
    }
    
    # Clear tracking
    $script:CreatedResources = @{
        ResourceGroup = $null
        Resources = @()
        DeploymentName = $null
        StartTime = $null
    }
}

Export-ModuleMember -Function Register-Resource, Initialize-RollbackTracking, Get-CreatedResources, Invoke-Rollback

