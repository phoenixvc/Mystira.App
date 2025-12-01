# Azure CLI Helper Functions
# Provides timeout protection and Azure CLI wrappers

function Invoke-AzCliWithTimeout {
    <#
    .SYNOPSIS
    Executes an Azure CLI command with a timeout.
    
    .PARAMETER Command
    The Azure CLI command to execute (without 'az' prefix)
    
    .PARAMETER TimeoutSeconds
    Maximum time to wait for the command (default: 30)
    
    .PARAMETER Verbose
    Show verbose output
    #>
    param(
        [string]$Command,
        [int]$TimeoutSeconds = 30,
        [switch]$Verbose
    )
    
    try {
        $job = Start-Job -ScriptBlock {
            param($cmd)
            $ErrorActionPreference = 'SilentlyContinue'
            $output = Invoke-Expression $cmd 2>&1 | Out-String
            return $output
        } -ArgumentList $Command
        
        $result = $job | Wait-Job -Timeout $TimeoutSeconds
        
        if ($result) {
            $output = $job | Receive-Job
            $job | Remove-Job -ErrorAction SilentlyContinue
            return $output.Trim()
        }
        else {
            $job | Stop-Job -ErrorAction SilentlyContinue
            $job | Remove-Job -ErrorAction SilentlyContinue
            if ($Verbose) {
                Write-Warning "Command timed out after $TimeoutSeconds seconds"
            }
            return $null
        }
    }
    catch {
        if ($Verbose) {
            Write-Warning "Error executing command: $($_.Exception.Message)"
        }
        return $null
    }
}

function Test-AzureLogin {
    <#
    .SYNOPSIS
    Checks if user is logged into Azure CLI.
    #>
    try {
        $account = az account show 2>$null | ConvertFrom-Json
        return $account
    }
    catch {
        return $null
    }
}

function Set-AzureSubscription {
    <#
    .SYNOPSIS
    Sets the active Azure subscription.
    
    .PARAMETER SubscriptionId
    The subscription ID to set
    #>
    param(
        [string]$SubscriptionId
    )
    
    try {
        az account set --subscription $SubscriptionId 2>$null | Out-Null
        return $true
    }
    catch {
        return $false
    }
}

Export-ModuleMember -Function Invoke-AzCliWithTimeout, Test-AzureLogin, Set-AzureSubscription

