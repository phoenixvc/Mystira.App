# Deployment State Helper Functions
# Provides state management for deployment retries and conflict resolution

function New-DeploymentState {
    <#
    .SYNOPSIS
    Creates a new deployment state object.
    #>
    param(
        [string]$Location,
        [string]$ResourceGroup,
        [string]$ResourcePrefix,
        [string]$ExpectedStorageName
    )
    
    return @{
        Location = $Location
        ResourceGroup = $ResourceGroup
        ResourcePrefix = $ResourcePrefix
        ExpectedStorageName = $ExpectedStorageName
        
        # Skip flags
        SkipCosmosCreation = $false
        SkipStorageCreation = $false
        SkipCommServiceCreation = $false
        SkipAppServiceCreation = $false
        
        # Existing resource references
        ExistingAppServiceResourceGroup = ""
        ExistingCosmosResourceGroup = ""
        ExistingCosmosDbAccountName = ""
        
        # Attempt flags
        CosmosUseAttempted = $false
        AppServiceUseAttempted = $false
        CommServiceUseAttempted = $false
        
        # Retry state
        RetryCount = 0
        ShouldRetry = $false
        MaxRetries = 3
    }
}

function Update-DeploymentState {
    <#
    .SYNOPSIS
    Updates deployment state with changes from conflict handlers.
    #>
    param(
        [hashtable]$State,
        [hashtable]$Changes
    )
    
    foreach ($key in $Changes.Keys) {
        if ($State.ContainsKey($key)) {
            $State[$key] = $Changes[$key]
        }
    }
    
    return $State
}

Export-ModuleMember -Function New-DeploymentState, Update-DeploymentState

