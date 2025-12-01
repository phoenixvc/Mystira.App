# Resource Group Helper Functions
# Provides functions for resource group operations

function Test-ResourceGroupExists {
    <#
    .SYNOPSIS
    Checks if a resource group exists.
    #>
    param(
        [string]$Name
    )
    
    try {
        if (Get-Command Invoke-AzureCliWithRetry -ErrorAction SilentlyContinue) {
            $result = Invoke-AzureCliWithRetry -Command "az group show --name `"$Name`" --output json" -MaxRetries 2
            if ($result) {
                $rgInfo = $result | ConvertFrom-Json -ErrorAction SilentlyContinue
                return $null -ne $rgInfo
            }
        } else {
            $rgInfo = az group show --name $Name --output json 2>$null | ConvertFrom-Json
            return $null -ne $rgInfo
        }
    }
    catch {
        return $false
    }
    
    return $false
}

function New-ResourceGroup {
    <#
    .SYNOPSIS
    Creates a new resource group.
    #>
    param(
        [string]$Name,
        [string]$Location
    )
    
    try {
        if (Get-Command Invoke-AzureCliWithRetry -ErrorAction SilentlyContinue) {
            $result = Invoke-AzureCliWithRetry -Command "az group create --name `"$Name`" --location `"$Location`" --output json" -MaxRetries 2
            if ($result) {
                $rgInfo = $result | ConvertFrom-Json -ErrorAction SilentlyContinue
                return @{
                    Success = $true
                    ResourceGroup = $rgInfo
                }
            }
        } else {
            az group create --name $Name --location $Location --output none 2>$null | Out-Null
            if ($LASTEXITCODE -eq 0) {
                return @{
                    Success = $true
                    ResourceGroup = $null
                }
            }
        }
    }
    catch {
        Write-Log "Failed to create resource group: $($_.Exception.Message)" "ERROR"
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
    
    return @{
        Success = $false
        Error = "Unknown error"
    }
}

function Get-ResourceGroupInfo {
    <#
    .SYNOPSIS
    Gets information about a resource group.
    #>
    param(
        [string]$Name
    )
    
    try {
        if (Get-Command Invoke-AzureCliWithRetry -ErrorAction SilentlyContinue) {
            $result = Invoke-AzureCliWithRetry -Command "az group show --name `"$Name`" --output json" -MaxRetries 2
            if ($result) {
                return $result | ConvertFrom-Json -ErrorAction SilentlyContinue
            }
        } else {
            $result = az group show --name $Name --output json 2>$null
            if ($result) {
                return $result | ConvertFrom-Json
            }
        }
    }
    catch {
        Write-Log "Failed to get resource group info: $($_.Exception.Message)" "WARN"
    }
    
    return $null
}

Export-ModuleMember -Function Test-ResourceGroupExists, New-ResourceGroup, Get-ResourceGroupInfo

