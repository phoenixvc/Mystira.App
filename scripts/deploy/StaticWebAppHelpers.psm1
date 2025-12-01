# Static Web App Helper Functions
# Provides functions for creating and managing Azure Static Web Apps

function Get-StaticWebAppSupportedRegions {
    <#
    .SYNOPSIS
    Returns the list of regions where Static Web Apps are available.
    #>
    return @("westus2", "centralus", "eastus2", "westeurope", "eastasia")
}

function Test-StaticWebAppRegion {
    <#
    .SYNOPSIS
    Checks if a region supports Static Web Apps.
    #>
    param(
        [string]$Region
    )
    
    $supportedRegions = Get-StaticWebAppSupportedRegions
    return $Region -in $supportedRegions
}

function Get-StaticWebAppFallbackRegion {
    <#
    .SYNOPSIS
    Gets a fallback region for Static Web Apps if the requested region is not supported.
    #>
    param(
        [string]$PreferredRegion = "westeurope"
    )
    
    $supportedRegions = Get-StaticWebAppSupportedRegions
    if ($PreferredRegion -in $supportedRegions) {
        return $PreferredRegion
    }
    return "westeurope"  # Default fallback
}

function Get-GitHubRepositoryInfo {
    <#
    .SYNOPSIS
    Extracts GitHub repository owner and name from git remote URL.
    #>
    param(
        [string]$RemoteUrl
    )
    
    if ($RemoteUrl -match "github\.com[:/]([^/]+)/([^/]+?)(?:\.git)?$") {
        return @{
            Owner = $matches[1]
            Name = $matches[2] -replace '\.git$', ''
            Success = $true
        }
    }
    
    return @{
        Owner = ""
        Name = ""
        Success = $false
    }
}

function Test-StaticWebAppExists {
    <#
    .SYNOPSIS
    Checks if a Static Web App exists.
    #>
    param(
        [string]$Name,
        [string]$ResourceGroup
    )
    
    try {
        if (Get-Command Invoke-AzureCliWithRetry -ErrorAction SilentlyContinue) {
            $result = Invoke-AzureCliWithRetry -Command "az staticwebapp show --name `"$Name`" --resource-group `"$ResourceGroup`" --output json" -MaxRetries 2
            if ($result) {
                $swaInfo = $result | ConvertFrom-Json -ErrorAction SilentlyContinue
                return $null -ne $swaInfo
            }
        } else {
            $swaInfo = az staticwebapp show --name $Name --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
            return $null -ne $swaInfo
        }
    }
    catch {
        return $false
    }
    
    return $false
}

function Connect-StaticWebAppToGitHub {
    <#
    .SYNOPSIS
    Connects a Static Web App to a GitHub repository using REST API.
    #>
    param(
        [string]$StaticWebAppName,
        [string]$ResourceGroup,
        [string]$SubscriptionId,
        [string]$RepositoryOwner,
        [string]$RepositoryName,
        [string]$Branch = "dev",
        [string]$AppLocation = "./src/Mystira.App.PWA",
        [string]$ApiLocation = "swa-db-connections",
        [string]$OutputLocation = "out",
        [string]$GitHubPat
    )
    
    try {
        $accessToken = az account get-access-token --resource "https://management.azure.com" --query accessToken -o tsv 2>$null
        $azAccount = az account show --output json 2>$null | ConvertFrom-Json
        
        if (-not $accessToken -or -not $azAccount) {
            return @{
                Success = $false
                Error = "Could not get Azure access token"
            }
        }
        
        $apiVersion = "2022-03-01"
        $uri = "https://management.azure.com/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroup/providers/Microsoft.Web/staticSites/$StaticWebAppName/sourcecontrols/GitHub?api-version=$apiVersion"
        
        $body = @{
            properties = @{
                repo = "https://github.com/$RepositoryOwner/$RepositoryName"
                branch = $Branch
                githubActionConfiguration = @{
                    generateWorkflowFile = $true
                    workflowSettings = @{
                        appLocation = $AppLocation
                        apiLocation = $ApiLocation
                        outputLocation = $OutputLocation
                    }
                }
                githubPersonalAccessToken = $GitHubPat
            }
        } | ConvertTo-Json -Depth 10
        
        $headers = @{
            "Authorization" = "Bearer $accessToken"
            "Content-Type" = "application/json"
        }
        
        $response = Invoke-RestMethod -Uri $uri -Method Put -Headers $headers -Body $body -ErrorAction Stop
        
        return @{
            Success = $true
            Response = $response
        }
    }
    catch {
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

Export-ModuleMember -Function Get-StaticWebAppSupportedRegions, Test-StaticWebAppRegion, Get-StaticWebAppFallbackRegion, Get-GitHubRepositoryInfo, Test-StaticWebAppExists, Connect-StaticWebAppToGitHub

