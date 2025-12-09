# Bicep Helper Functions
# Provides functions for Bicep template operations

function New-BicepParameterFile {
    <#
    .SYNOPSIS
    Creates a Bicep parameter file with the specified parameters.
    #>
    param(
        [hashtable]$Parameters,
        [string]$OutputPath
    )
    
    try {
        $paramsContent = @{
            contentVersion = "1.0.0.0"
            parameters = @{}
        }
        
        # Add $schema using bracket notation to avoid parser issues
        $paramsContent['$schema'] = "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#"
        
        foreach ($key in $Parameters.Keys) {
            $paramsContent.parameters[$key] = @{
                value = $Parameters[$key]
            }
        }
        
        $jsonContent = $paramsContent | ConvertTo-Json -Depth 10
        $jsonContent | Out-File -FilePath $OutputPath -Encoding UTF8 -Force
        
        return @{
            Success = $true
            Path = $OutputPath
        }
    }
    catch {
        Write-Log "Failed to create parameter file: $($_.Exception.Message)" "ERROR"
        return @{
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

function Find-BicepTemplate {
    <#
    .SYNOPSIS
    Finds a Bicep template file in common locations.
    #>
    param(
        [string]$TemplateName = "main.bicep",
        [string]$BasePath = "."
    )
    
    $searchPaths = @(
        Join-Path $BasePath "infrastructure\dev\$TemplateName",
        Join-Path $BasePath "infrastructure\$TemplateName",
        Join-Path $BasePath $TemplateName,
        Join-Path $BasePath "bicep\$TemplateName"
    )
    
    foreach ($path in $searchPaths) {
        if (Test-Path $path) {
            return $path
        }
    }
    
    return $null
}

function Invoke-BicepDeployment {
    <#
    .SYNOPSIS
    Executes a Bicep deployment.
    #>
    param(
        [string]$ResourceGroup,
        [string]$TemplateFile,
        [string]$ParameterFile,
        [string]$DeploymentName,
        [int]$TimeoutSeconds = 3600
    )
    
    try {
        $command = "az deployment group create --resource-group `"$ResourceGroup`" --template-file `"$TemplateFile`" --parameters `"@$ParameterFile`" --name `"$DeploymentName`" --output json"
        
        if (Get-Command Invoke-AzureCliWithRetry -ErrorAction SilentlyContinue) {
            $result = Invoke-AzureCliWithRetry -Command $command -MaxRetries 1 -TimeoutSeconds $TimeoutSeconds
            if ($result) {
                return @{
                    Success = $true
                    Output = $result
                }
            }
        } else {
            $output = Invoke-Expression $command 2>&1
            if ($LASTEXITCODE -eq 0) {
                return @{
                    Success = $true
                    Output = $output
                }
            } else {
                return @{
                    Success = $false
                    Error = ($output -join "`n")
                }
            }
        }
    }
    catch {
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

Export-ModuleMember -Function New-BicepParameterFile, Find-BicepTemplate, Invoke-BicepDeployment

