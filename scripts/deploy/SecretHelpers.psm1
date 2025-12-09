# Secret Helper Functions
# Provides functions for retrieving and managing secrets (JWT, connection strings, etc.)

function Get-ExistingJwtSecret {
    <#
    .SYNOPSIS
    Retrieves existing JWT secret from App Service app settings.
    #>
    param(
        [string]$ResourceGroup,
        [string[]]$AppServiceNames = @()
    )
    
    $jwtSecret = $null
    
    # If no app service names provided, find all App Services in the resource group
    if ($AppServiceNames.Count -eq 0) {
        try {
            if (Get-Command Invoke-AzureCliWithRetry -ErrorAction SilentlyContinue) {
                $appServicesJson = Invoke-AzureCliWithRetry -Command "az resource list --resource-group `"$ResourceGroup`" --resource-type `"Microsoft.Web/sites`" --output json" -MaxRetries 2
                if ($appServicesJson) {
                    $appServices = $appServicesJson | ConvertFrom-Json -ErrorAction SilentlyContinue
                    if ($appServices) {
                        $AppServiceNames = $appServices | ForEach-Object { $_.name }
                    }
                }
            } else {
                $appServices = az resource list --resource-group $ResourceGroup --resource-type "Microsoft.Web/sites" --output json 2>$null | ConvertFrom-Json
                if ($appServices) {
                    $AppServiceNames = $appServices | ForEach-Object { $_.name }
                }
            }
        }
        catch {
            Write-Log "Failed to list App Services: $($_.Exception.Message)" "WARN"
        }
    }
    
    # Check each App Service for JWT secret
    foreach ($appServiceName in $AppServiceNames) {
        try {
            Write-Log "Checking App Service '$appServiceName' for JWT secret" "INFO"
            
            if (Get-Command Invoke-AzureCliWithRetry -ErrorAction SilentlyContinue) {
                $appSettingsJson = Invoke-AzureCliWithRetry -Command "az webapp config appsettings list --name `"$appServiceName`" --resource-group `"$ResourceGroup`" --output json" -MaxRetries 2
                if ($appSettingsJson) {
                    $appSettings = $appSettingsJson | ConvertFrom-Json -ErrorAction SilentlyContinue
                }
            } else {
                $appSettings = az webapp config appsettings list --name $appServiceName --resource-group $ResourceGroup --output json 2>$null | ConvertFrom-Json
            }
            
            if ($appSettings) {
                # Check for Jwt__Key (preferred) or JwtSettings__SecretKey
                $jwtKey = $appSettings | Where-Object { $_.name -eq "Jwt__Key" -or $_.name -eq "JwtSettings__SecretKey" }
                
                if ($jwtKey -and $jwtKey.value) {
                    $jwtSecret = $jwtKey.value
                    Write-Log "Found existing JWT secret in App Service '$appServiceName'" "INFO"
                    break
                }
            }
        }
        catch {
            Write-Log "Failed to retrieve app settings from ${appServiceName}: $($_.Exception.Message)" "WARN"
        }
    }
    
    return $jwtSecret
}

function New-JwtSecret {
    <#
    .SYNOPSIS
    Generates a new JWT secret key.
    #>
    param(
        [int]$Length = 32
    )
    
    $jwtSecret = -join ((65..90) + (97..122) + (48..57) | Get-Random -Count $Length | ForEach-Object { [char]$_ })
    $jwtSecretBase64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($jwtSecret))
    
    return @{
        PlainText = $jwtSecret
        Base64 = $jwtSecretBase64
    }
}

Export-ModuleMember -Function Get-ExistingJwtSecret, New-JwtSecret

