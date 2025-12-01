# Deployment Orchestration Helper Functions
# Provides functions for orchestrating Bicep deployments with conflict handling

function Invoke-InfrastructureDeploymentWithRetry {
    <#
    .SYNOPSIS
    Orchestrates infrastructure deployment with retry logic and conflict handling.
    #>
    param(
        [string]$ResourceGroup,
        [string]$Location,
        [string]$BicepFile,
        [string]$ResourcePrefix,
        [string]$ExpectedStorageName,
        [string]$JwtSecretBase64,
        [string]$DeploymentName,
        [int]$MaxRetries = 3,
        [switch]$Verbose,
        [switch]$WhatIf
    )
    
    $result = @{
        Success = $false
        Error = ""
        DeploymentOutput = $null
        FinalLocation = $Location
        FinalResourceGroup = $ResourceGroup
        FinalResourcePrefix = $ResourcePrefix
        FinalExpectedStorageName = $ExpectedStorageName
    }
    
    # Initialize state
    $retryCount = 0
    $cosmosUseAttempted = $false
    $skipCosmosCreation = $false
    $skipStorageCreation = $false
    $skipCommServiceCreation = $false
    $skipAppServiceCreation = $false
    $appServiceUseAttempted = $false
    $commServiceUseAttempted = $false
    $existingAppServiceResourceGroup = ""
    $existingCosmosResourceGroup = ""
    $existingCosmosDbAccountName = ""
    
    $currentLocation = $Location
    $currentRG = $ResourceGroup
    $currentResourcePrefix = $ResourcePrefix
    $currentExpectedStorageName = $ExpectedStorageName
    
    while ($retryCount -le $MaxRetries) {
        # Ensure resource group exists in current region
        $rgCheck = az group show --name $currentRG 2>$null | ConvertFrom-Json -ErrorAction SilentlyContinue
        if (-not $rgCheck) {
            Write-Host "Creating resource group $currentRG in $currentLocation..." -NoNewline
            az group create --name $currentRG --location $currentLocation --output none 2>$null
            Write-Host " [OK]" -ForegroundColor Green
        }
        
        # Create parameters JSON file for secure parameters
        $paramsFile = Join-Path $env:TEMP "bicep-params-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
        
        # Build parameters object
        if ($Verbose -and $retryCount -gt 0) {
            Write-ColorOutput Yellow "   [VERBOSE] Retry $retryCount : skipAppServiceCreation=$skipAppServiceCreation, existingAppServiceResourceGroup=$existingAppServiceResourceGroup"
        }
        
        $paramsObj = @{
            environment             = @{ value = "dev" }
            location                = @{ value = $currentLocation }
            resourcePrefix          = @{ value = $currentResourcePrefix }
            jwtSecretKey            = @{ value = $JwtSecretBase64 }
            newStorageAccountName   = @{ value = $currentExpectedStorageName }
            skipCosmosCreation      = @{ value = [bool]$skipCosmosCreation }
            skipStorageCreation     = @{ value = [bool]$skipStorageCreation }
            skipCommServiceCreation = @{ value = [bool]$skipCommServiceCreation }
            skipAppServiceCreation  = @{ value = [bool]$skipAppServiceCreation }
        }
        
        # Add existing resource parameters
        $paramsObj.existingAppServiceResourceGroup = @{ value = $existingAppServiceResourceGroup }
        $paramsObj.existingCosmosResourceGroup = @{ value = $existingCosmosResourceGroup }
        $paramsObj.existingCosmosDbAccountName = @{ value = $existingCosmosDbAccountName }
        
        # Create parameter file using helper
        $paramResult = New-BicepParameterFile -Parameters $paramsObj -OutputPath $paramsFile
        if (-not $paramResult.Success) {
            $result.Error = "Failed to create parameter file: $($paramResult.Error)"
            return $result
        }
        
        Write-Output "Using parameters file: $paramsFile"
        Write-Output ""
        
        if ($WhatIf) {
            Write-ColorOutput Cyan "[WHATIF] Would deploy infrastructure with the following parameters:"
            $paramsDebug = Get-Content $paramsFile | ConvertFrom-Json
            Write-Output ($paramsDebug.parameters | ConvertTo-Json -Depth 10)
            Write-Output ""
            Write-ColorOutput Yellow "   [WHATIF] Deployment would target:"
            Write-Output "   Resource Group: $currentRG"
            Write-Output "   Location: $currentLocation"
            Write-Output "   Template: $BicepFile"
            Write-Output ""
            Write-ColorOutput Green "SUCCESS: [WHATIF] Preview complete. Run without -WhatIf to deploy."
            $result.Success = $true
            break
        }
        
        Write-ColorOutput Cyan "Starting infrastructure deployment..."
        Write-Output "This may take several minutes. Please wait..."
        Write-Output ""
        Write-Log "Starting infrastructure deployment" "INFO"
        Write-Log "Resource Group: $currentRG, Location: $currentLocation, Template: $BicepFile" "INFO"
        
        $deploymentSuccess = $false
        $shouldRetry = $false
        $deploymentStartTime = Get-Date
        
        try {
            Write-Log "Step: Executing Bicep deployment" "INFO"
            if ($Verbose) {
                Write-ColorOutput Yellow "   [VERBOSE] Parameters file contents:"
                $paramsDebug = Get-Content $paramsFile | ConvertFrom-Json
                Write-Output ($paramsDebug.parameters | ConvertTo-Json -Depth 10)
                Write-Output ""
            }
            
            # Show progress indicator
            Write-Host "Deploying..." -NoNewline
            $progressJob = Start-Job -ScriptBlock {
                $dots = 0
                while ($true) {
                    Start-Sleep -Seconds 2
                    $dots = ($dots + 1) % 4
                    $progress = "." * $dots + " " * (3 - $dots)
                    Write-Host "`rDeploying$progress" -NoNewline
                }
            }
            
            # Capture both stdout and stderr separately with retry logic for transient errors
            $deploymentResult = Invoke-WithRetry -ScriptBlock {
                $output = az deployment group create `
                    --resource-group $currentRG `
                    --template-file $BicepFile `
                    --parameters "@$paramsFile" `
                    --mode Incremental `
                    --name $DeploymentName `
                    --output json 2>&1 | Where-Object { $_ -notmatch '^WARNING:' }
                
                if ($LASTEXITCODE -ne 0) {
                    throw ($output -join "`n")
                }
                
                return $output
            } -MaxRetries 3 -InitialDelaySeconds 5 -MaxDelaySeconds 30
            
            if (-not $deploymentResult.Success) {
                if ($deploymentResult.IsTransient) {
                    Write-ColorOutput Yellow "WARNING: Transient error detected, will retry with conflict handling..."
                    $stdout = $deploymentResult.Error
                }
                else {
                    $stdout = $deploymentResult.Error
                }
                $LASTEXITCODE = 1
            }
            else {
                $stdout = $deploymentResult.Result
                $LASTEXITCODE = 0
            }
            
            # Stop progress indicator
            $progressJob | Stop-Job -ErrorAction SilentlyContinue
            $progressJob | Remove-Job -ErrorAction SilentlyContinue
            Write-Host "`r" -NoNewline  # Clear progress line
            
            if ($LASTEXITCODE -eq 0) {
                # Success - try to parse JSON
                $elapsed = (Get-Date) - $deploymentStartTime
                Write-Host "[OK] Deployment completed in $([math]::Round($elapsed.TotalMinutes, 1)) minutes" -ForegroundColor Green
                Write-Log "Step: Deployment succeeded in $([math]::Round($elapsed.TotalSeconds, 2)) seconds" "INFO"
                try {
                    $deploymentOutput = $stdout | ConvertFrom-Json -ErrorAction Stop
                    if (-not $Verbose) {
                        Write-Output "Deployment successful!"
                    }
                    else {
                        Write-Output ($deploymentOutput | ConvertTo-Json -Depth 10 | Out-String)
                    }
                    
                    # Track created resources from deployment output
                    if ($deploymentOutput.properties.outputs) {
                        if ($deploymentOutput.properties.outputs.storageAccountName) {
                            Register-Resource -ResourceType "Microsoft.Storage/storageAccounts" `
                                -ResourceName $deploymentOutput.properties.outputs.storageAccountName.value `
                                -ResourceGroup $currentRG
                        }
                        if ($deploymentOutput.properties.outputs.cosmosDbAccountName) {
                            Register-Resource -ResourceType "Microsoft.DocumentDB/databaseAccounts" `
                                -ResourceName $deploymentOutput.properties.outputs.cosmosDbAccountName.value `
                                -ResourceGroup $currentRG
                        }
                        if ($deploymentOutput.properties.outputs.communicationServiceName) {
                            Register-Resource -ResourceType "Microsoft.Communication/communicationServices" `
                                -ResourceName $deploymentOutput.properties.outputs.communicationServiceName.value `
                                -ResourceGroup $currentRG
                        }
                    }
                    
                    $result.Success = $true
                    $result.DeploymentOutput = $deploymentOutput
                    $deploymentSuccess = $true
                }
                catch {
                    # If JSON parse fails but exit code is 0, deployment might have succeeded
                    Write-Output "Deployment completed (output parsing skipped)"
                    $result.Success = $true
                    $deploymentSuccess = $true
                }
            }
            else {
                # Failure - try to parse error JSON
                $elapsed = (Get-Date) - $deploymentStartTime
                Write-Log "Step: Deployment failed after $([math]::Round($elapsed.TotalSeconds, 2)) seconds" "ERROR"
                $errorJson = $null
                $errorMsg = ""
                
                try {
                    $errorJson = $stdout | ConvertFrom-Json -ErrorAction Stop
                    if ($errorJson.error) {
                        $errorMsg = $errorJson.error.message
                        Write-Log "Error details: $errorMsg" "ERROR"
                    }
                }
                catch {
                    # Not JSON, use raw output
                    $errorMsg = ($stdout -join "`n")
                    Write-Log "Error (non-JSON): $errorMsg" "ERROR"
                }
                
                if (-not $errorMsg) {
                    $errorMsg = ($stdout -join "`n")
                    Write-Log "Error (raw): $errorMsg" "ERROR"
                }
                
                # Create paramsContent for conflict handlers (they need it to regenerate the file)
                $paramsContent = @{ parameters = $paramsObj }
                
                # Handle conflicts using existing handlers (they use ref parameters)
                $conflictHandled = Handle-DeploymentConflicts `
                    -ErrorMsg $errorMsg `
                    -ErrorOutput $stdout `
                    -ResourcePrefix $currentResourcePrefix `
                    -ResourceGroup ([ref]$currentRG) `
                    -Location ([ref]$currentLocation) `
                    -ResourcePrefixRef ([ref]$currentResourcePrefix) `
                    -ExpectedStorageName ([ref]$currentExpectedStorageName) `
                    -ParamsObj ([ref]$paramsObj) `
                    -ParamsContent ([ref]$paramsContent) `
                    -ParamsFile $paramsFile `
                    -ShouldRetry ([ref]$shouldRetry) `
                    -RetryCount ([ref]$retryCount) `
                    -SkipCosmosCreation ([ref]$skipCosmosCreation) `
                    -SkipStorageCreation ([ref]$skipStorageCreation) `
                    -SkipCommServiceCreation ([ref]$skipCommServiceCreation) `
                    -SkipAppServiceCreation ([ref]$skipAppServiceCreation) `
                    -ExistingAppServiceResourceGroup ([ref]$existingAppServiceResourceGroup) `
                    -ExistingCosmosResourceGroup ([ref]$existingCosmosResourceGroup) `
                    -ExistingCosmosDbAccountName ([ref]$existingCosmosDbAccountName) `
                    -CosmosUseAttempted ([ref]$cosmosUseAttempted) `
                    -AppServiceUseAttempted ([ref]$appServiceUseAttempted) `
                    -CommServiceUseAttempted ([ref]$commServiceUseAttempted) `
                    -Verbose:$Verbose `
                    -WhatIf:$WhatIf
                
                if (-not $conflictHandled) {
                    # Check if we need to offer rollback
                    $createdResources = Get-CreatedResources
                    if ($createdResources.Resources.Count -gt 0) {
                        Write-Output ""
                        Write-ColorOutput Yellow "WARNING: Would you like to rollback created resources? (y/n)"
                        $rollbackChoice = Read-Host
                        if ($rollbackChoice -eq 'y' -or $rollbackChoice -eq 'Y') {
                            Invoke-Rollback -Confirm:$false
                        }
                    }
                    $result.Error = $errorMsg
                }
            }
        }
        catch {
            $result.Error = $_.Exception.Message
            Write-Log "Exception at step: Infrastructure deployment - $($_.Exception.Message)" "ERROR"
        }
        finally {
            # Clean up temp file
            if (Test-Path $paramsFile) {
                Remove-Item $paramsFile -Force -ErrorAction SilentlyContinue
            }
        }
        
        # Update result with current state
        $result.FinalLocation = $currentLocation
        $result.FinalResourceGroup = $currentRG
        $result.FinalResourcePrefix = $currentResourcePrefix
        $result.FinalExpectedStorageName = $currentExpectedStorageName
        
        # If deployment succeeded, break the loop
        if ($deploymentSuccess) {
            break
        }
        
        # If we should retry, increment and continue
        if ($shouldRetry -and $retryCount -lt $MaxRetries) {
            $retryCount++
            Write-ColorOutput Yellow "Retrying deployment (attempt $retryCount of $MaxRetries)..."
            Start-Sleep -Seconds 2
        }
        else {
            break
        }
        
        # If we've exceeded max retries
        if ($retryCount -gt $MaxRetries) {
            Write-ColorOutput Red "ERROR: Maximum retry attempts exceeded ($MaxRetries)."
            if (-not $result.Error) {
                $result.Error = "Maximum retry attempts exceeded"
            }
            break
        }
    }
    
    return $result
}

function Handle-DeploymentConflicts {
    <#
    .SYNOPSIS
    Handles deployment conflicts by routing to appropriate conflict handlers.
    #>
    param(
        [string]$ErrorMsg,
        [array]$ErrorOutput,
        [string]$ResourcePrefix,
        [ref]$ResourceGroup,
        [ref]$Location,
        [ref]$ResourcePrefixRef,
        [ref]$ExpectedStorageName,
        [ref]$ParamsObj,
        [ref]$ParamsContent,
        [string]$ParamsFile,
        [ref]$ShouldRetry,
        [ref]$RetryCount,
        [ref]$SkipCosmosCreation,
        [ref]$SkipStorageCreation,
        [ref]$SkipCommServiceCreation,
        [ref]$SkipAppServiceCreation,
        [ref]$ExistingAppServiceResourceGroup,
        [ref]$ExistingCosmosResourceGroup,
        [ref]$ExistingCosmosDbAccountName,
        [ref]$CosmosUseAttempted,
        [ref]$AppServiceUseAttempted,
        [ref]$CommServiceUseAttempted,
        [switch]$Verbose,
        [switch]$WhatIf
    )
    
    $handled = $false
    
    # If we already tried "use" for Communication Service and it failed again, don't retry
    if ($CommServiceUseAttempted.Value -and $ErrorMsg -match "NameReservationTaken" -and $ErrorMsg -match "communication") {
        Write-Output ""
        $formattedError = Format-AzureError -ErrorJson ($ErrorOutput -join "`n") -Step "Communication Service conflict resolution"
        Write-ColorOutput Red $formattedError
        Write-Log "Communication Service 'use' failed after retry" "ERROR"
        
        # Offer rollback
        $createdResources = Get-CreatedResources
        if ($createdResources.Resources.Count -gt 0) {
            Write-Output ""
            Write-ColorOutput Yellow "WARNING: Would you like to rollback created resources? (y/n)"
            $rollbackChoice = Read-Host
            if ($rollbackChoice -eq 'y' -or $rollbackChoice -eq 'Y') {
                Invoke-Rollback -Confirm:$false
            }
        }
        
        return $false
    }
    
    # Check for App Service errors FIRST (before handling other conflicts)
    if ($AppServiceUseAttempted.Value -and ($ErrorMsg -match "Website.*already exists" -or ($ErrorMsg -match "Conflict" -and $ErrorMsg -match "Website"))) {
        Write-Output ""
        $formattedError = Format-AzureError -ErrorJson ($ErrorOutput -join "`n") -Step "App Service conflict resolution"
        Write-ColorOutput Red $formattedError
        Write-ColorOutput Yellow "   The App Services exist but the deployment is still trying to create them."
        Write-ColorOutput Cyan "   This may indicate the skipAppServiceCreation parameter isn't being applied correctly."
        Write-ColorOutput Cyan "   Parameter should be: skipAppServiceCreation=true, existingAppServiceResourceGroup=$($ResourceGroup.Value)"
        Write-Log "App Service 'use existing' failed after retry" "ERROR"
        
        # Offer rollback
        $createdResources = Get-CreatedResources
        if ($createdResources.Resources.Count -gt 0) {
            Write-Output ""
            Write-ColorOutput Yellow "WARNING: Would you like to rollback created resources? (y/n)"
            $rollbackChoice = Read-Host
            if ($rollbackChoice -eq 'y' -or $rollbackChoice -eq 'Y') {
                Invoke-Rollback -Confirm:$false
            }
        }
        
        return $false
    }
    
    # Handle App Service conflicts
    if ($ErrorMsg -match "Website.*already exists" -or ($ErrorMsg -match "Conflict" -and $ErrorMsg -match "Website")) {
        Handle-AppServiceConflict `
            -ErrorMsg $ErrorMsg `
            -ResourcePrefix $ResourcePrefix `
            -RG $ResourceGroup.Value `
            -Location $Location `
            -ResourceGroup $ResourceGroup `
            -ResourcePrefixRef $ResourcePrefixRef `
            -ParamsObj $ParamsObj `
            -ParamsContent $ParamsContent `
            -ParamsFile $ParamsFile `
            -ShouldRetry $ShouldRetry `
            -RetryCount $RetryCount `
            -SkipAppServiceCreation $SkipAppServiceCreation `
            -ExistingAppServiceResourceGroup $ExistingAppServiceResourceGroup `
            -AppServiceUseAttempted $AppServiceUseAttempted `
            -Verbose:$Verbose `
            -WhatIf:$WhatIf
        $handled = $true
    }
    # Handle storage account conflict
    elseif ($ErrorMsg -match "StorageAccountAlreadyTaken" -or $ErrorMsg -match "storage account.*already taken") {
        Handle-StorageConflict `
            -Location $Location `
            -ResourceGroup $ResourceGroup `
            -ResourcePrefix $ResourcePrefixRef `
            -ExpectedStorageName $ExpectedStorageName `
            -ParamsObj $ParamsObj `
            -ParamsContent $ParamsContent `
            -ParamsFile $ParamsFile `
            -ShouldRetry $ShouldRetry `
            -RetryCount $RetryCount `
            -WhatIf:$WhatIf
        $handled = $true
    }
    # Handle Communication Services conflict
    elseif ($ErrorMsg -match "NameReservationTaken" -and $ErrorMsg -match "communication") {
        Handle-CommunicationServiceConflict `
            -ResourcePrefix $ResourcePrefix `
            -RG $ResourceGroup.Value `
            -Location $Location `
            -ResourceGroup $ResourceGroup `
            -ResourcePrefixRef $ResourcePrefixRef `
            -ExpectedStorageName $ExpectedStorageName `
            -ParamsObj $ParamsObj `
            -ParamsContent $ParamsContent `
            -ParamsFile $ParamsFile `
            -ShouldRetry $ShouldRetry `
            -RetryCount $RetryCount `
            -CommServiceUseAttempted $CommServiceUseAttempted `
            -Verbose:$Verbose `
            -WhatIf:$WhatIf
        $handled = $true
    }
    # Handle Cosmos DB conflict or region issues
    elseif (($ErrorMsg -match "Dns record.*already taken" -or ($ErrorMsg -match "BadRequest" -and $ErrorMsg -match "cosmos") -or ($ErrorMsg -match "ServiceUnavailable" -and $ErrorMsg -match "cosmos") -or ($ErrorMsg -match "high demand" -and $ErrorMsg -match "region") -or ($ErrorMsg -match "failed provisioning state")) -and -not $CosmosUseAttempted.Value) {
        Handle-CosmosDbConflict `
            -ErrorMsg $ErrorMsg `
            -ResourcePrefix $ResourcePrefix `
            -RG $ResourceGroup.Value `
            -ParamsObj $ParamsObj `
            -ParamsContent $ParamsContent `
            -ParamsFile $ParamsFile `
            -ShouldRetry $ShouldRetry `
            -RetryCount $RetryCount `
            -SkipCosmosCreation $SkipCosmosCreation `
            -CosmosUseAttempted $CosmosUseAttempted `
            -Verbose:$Verbose `
            -WhatIf:$WhatIf
        $handled = $true
    }
    # If we already tried using existing Cosmos DB and it failed again
    elseif ($CosmosUseAttempted.Value -and -not $AppServiceUseAttempted.Value -and ($ErrorMsg -match "cosmos" -or $ErrorMsg -match "Cosmos" -or $ErrorMsg -match "DocumentDB") -and -not ($ErrorMsg -match "Website" -or $ErrorMsg -match "App Service" -or $ErrorMsg -match "appservice" -or $ErrorMsg -match "Conflict.*Website")) {
        Write-Output ""
        $formattedError = Format-AzureError -ErrorJson ($ErrorOutput -join "`n") -Step "Cosmos DB 'use existing' failed"
        Write-ColorOutput Red $formattedError
        Write-Log "Cosmos DB 'use existing' failed. Cannot proceed." "ERROR"
        
        # Offer rollback
        $createdResources = Get-CreatedResources
        if ($createdResources.Resources.Count -gt 0) {
            Write-Output ""
            Write-ColorOutput Yellow "WARNING: Would you like to rollback created resources? (y/n)"
            $rollbackChoice = Read-Host
            if ($rollbackChoice -eq 'y' -or $rollbackChoice -eq 'Y') {
                Invoke-Rollback -Confirm:$false
            }
        }
        
        return $false
    }
    
    return $handled
}

Export-ModuleMember -Function Invoke-InfrastructureDeploymentWithRetry, Handle-DeploymentConflicts
