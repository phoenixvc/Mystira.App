# Static Web App Creation Helper Functions
# Provides functions for creating and connecting Static Web Apps

function New-StaticWebAppWithGitHub {
    <#
    .SYNOPSIS
    Creates a Static Web App and connects it to GitHub.
    #>
    param(
        [string]$Name,
        [string]$ResourceGroup,
        [string]$Location,
        [string]$SubscriptionId,
        [string]$RepositoryOwner,
        [string]$RepositoryName,
        [string]$Branch = "dev",
        [string]$AppLocation = "./src/Mystira.App.PWA",
        [string]$ApiLocation = "swa-db-connections",
        [string]$OutputLocation = "out",
        [string]$GitHubPat,
        [string]$SwaCliPath = $null,
        [switch]$SwaCliAvailable,
        [switch]$Verbose
    )
    
    $result = @{
        Success = $false
        Created = $false
        Error = ""
        StaticWebApp = $null
    }
    
    # Check if name is available
    $nameAvailable = $true
    try {
        $existingSWA = az staticwebapp list --query "[?name=='$Name']" -o json 2>$null | ConvertFrom-Json
        if ($existingSWA -and $existingSWA.Count -gt 0) {
            $nameAvailable = $false
            Write-ColorOutput Yellow "WARNING: Static Web App name '$Name' is already taken globally."
            Write-Log "Static Web App name unavailable: $Name" "WARN"
        }
    }
    catch {
        # Assume available if check fails
    }
    
    if (-not $nameAvailable) {
        $result.Error = "Static Web App name is already taken"
        return $result
    }
    
    # Get GitHub repo info if not provided
    if (-not $RepositoryOwner -or -not $RepositoryName) {
        $repoUrl = Get-GitRemoteUrl
        $repoInfo = Get-GitHubRepositoryInfo -RemoteUrl $repoUrl
        $RepositoryOwner = $repoInfo.Owner
        $RepositoryName = $repoInfo.Name
    }
    
    if (-not $RepositoryOwner -or -not $RepositoryName) {
        $result.Error = "Could not detect GitHub repository"
        Write-ColorOutput Yellow "WARNING: Could not detect GitHub repository."
        Write-Log "Error: Could not detect GitHub repository" "ERROR"
        return $result
    }
    
    Write-Output "Repository: $RepositoryOwner/$RepositoryName"
    Write-Log "GitHub repository: $RepositoryOwner/$RepositoryName" "INFO"
    Write-Output ""
    
    # Use SWA CLI if available (better GitHub PAT support)
    if ($SwaCliAvailable -and $GitHubPat -and $RepositoryOwner -and $RepositoryName) {
        $swaResult = New-StaticWebAppWithSwaCli `
            -Name $Name `
            -ResourceGroup $ResourceGroup `
            -Location $Location `
            -SubscriptionId $SubscriptionId `
            -RepositoryOwner $RepositoryOwner `
            -RepositoryName $RepositoryName `
            -Branch $Branch `
            -AppLocation $AppLocation `
            -ApiLocation $ApiLocation `
            -OutputLocation $OutputLocation `
            -GitHubPat $GitHubPat `
            -SwaCliPath $SwaCliPath `
            -Verbose:$Verbose
        
        if ($swaResult.Success) {
            $result.Success = $true
            $result.Created = $true
            $result.StaticWebApp = $swaResult.StaticWebApp
            return $result
        }
        
        # If SWA CLI failed, fall through to Azure CLI
        Write-ColorOutput Yellow "WARNING: SWA CLI creation failed, trying Azure CLI..."
    }
    
    # Fallback to Azure CLI
    if (-not $SwaCliAvailable) {
        Write-ColorOutput Yellow "INFO: SWA CLI not found. Using Azure CLI (install with: npm install -g @azure/static-web-apps-cli)"
    }
    
    $swaResult = New-StaticWebAppWithAzureCli `
        -Name $Name `
        -ResourceGroup $ResourceGroup `
        -Location $Location `
        -SubscriptionId $SubscriptionId `
        -RepositoryOwner $RepositoryOwner `
        -RepositoryName $RepositoryName `
        -Branch $Branch `
        -AppLocation $AppLocation `
        -ApiLocation $ApiLocation `
        -OutputLocation $OutputLocation `
        -GitHubPat $GitHubPat `
        -Verbose:$Verbose
    
    if ($swaResult.Success) {
        $result.Success = $true
        $result.Created = $true
        $result.StaticWebApp = $swaResult.StaticWebApp
    } else {
        $result.Error = $swaResult.Error
    }
    
    return $result
}

function New-StaticWebAppWithSwaCli {
    <#
    .SYNOPSIS
    Creates a Static Web App using SWA CLI.
    #>
    param(
        [string]$Name,
        [string]$ResourceGroup,
        [string]$Location,
        [string]$SubscriptionId,
        [string]$RepositoryOwner,
        [string]$RepositoryName,
        [string]$Branch,
        [string]$AppLocation,
        [string]$ApiLocation,
        [string]$OutputLocation,
        [string]$GitHubPat,
        [string]$SwaCliPath = $null,
        [switch]$Verbose
    )
    
    $result = @{
        Success = $false
        StaticWebApp = $null
        Error = ""
    }
    
    try {
        # Static Web Apps have limited region support
        $swaLocation = Get-StaticWebAppFallbackRegion -PreferredRegion $Location
        
        if (-not (Test-StaticWebAppRegion -Region $Location)) {
            Write-ColorOutput Yellow "WARNING: Static Web Apps not available in $Location"
            $supportedRegions = Get-StaticWebAppSupportedRegions
            Write-ColorOutput Yellow "   Available regions: $($supportedRegions -join ', ')"
            Write-ColorOutput Cyan "   Creating Static Web App in '$swaLocation' (closest supported region)"
            Write-Log "Static Web Apps not available in $Location, using $swaLocation" "WARN"
        }
        
        # Determine SWA CLI command
        $swaCmd = if ($SwaCliPath -and (Test-Path $SwaCliPath)) {
            $SwaCliPath
        } else {
            # Try to find it in common locations
            $foundPath = $null
            $checkPaths = @(
                "$env:APPDATA\npm\swa.cmd",
                "$env:LOCALAPPDATA\npm\swa.cmd"
            )
            $npmPrefix = npm config get prefix 2>$null
            if ($npmPrefix) {
                $checkPaths += @(
                    Join-Path $npmPrefix "swa.cmd",
                    Join-Path $npmPrefix "node_modules\@azure\static-web-apps-cli\dist\swa.cmd"
                )
            }
            foreach ($path in $checkPaths) {
                if ($path -and (Test-Path $path)) {
                    $foundPath = $path
                    break
                }
            }
            if ($foundPath) { $foundPath } else { "swa" }
        }
        
        Write-Log "Using SWA CLI at: $swaCmd" "INFO"
        
        # Set GitHub token as environment variable for SWA CLI
        $env:GITHUB_TOKEN = $GitHubPat
        
        # Ensure SWA CLI is logged in
        Write-ColorOutput Cyan "   Authenticating with SWA CLI..."
        $swaLoginOutput = & $swaCmd login 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-ColorOutput Yellow "   WARNING: SWA CLI login may have failed, but continuing..."
            Write-Log "SWA CLI login output: $($swaLoginOutput -join '`n')" "WARN"
        }
        
        # Create a minimal output directory for deployment
        $tempDeployDir = Join-Path $env:TEMP "swa-deploy-$(Get-Random)"
        New-Item -ItemType Directory -Path $tempDeployDir -Force | Out-Null
        try {
            # Create a minimal index.html for deployment
            "<!DOCTYPE html><html><head><title>SWA Setup</title></head><body><h1>Setting up Static Web App</h1></body></html>" | Out-File -FilePath (Join-Path $tempDeployDir "index.html") -Encoding UTF8
            
            Write-ColorOutput Cyan "   Creating Static Web App and connecting to GitHub with SWA CLI..."
            Write-Log "Using SWA CLI deploy to create SWA and connect GitHub" "INFO"
            
            # Use SWA CLI deploy with all parameters
            $swaDeployOutput = & $swaCmd deploy $tempDeployDir `
                --app-name $Name `
                --resource-group $ResourceGroup `
                --subscription-id $SubscriptionId `
                --repo "https://github.com/$RepositoryOwner/$RepositoryName" `
                --branch $Branch `
                --app-location $AppLocation `
                --api-location $ApiLocation `
                --output-location $OutputLocation `
                2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "[OK]" -ForegroundColor Green
                Write-ColorOutput Green "SUCCESS: Static Web App created and connected to GitHub via SWA CLI!"
                Write-Log "Static Web App created and connected via SWA CLI" "INFO"
                $result.Success = $true
                $result.StaticWebApp = @{ name = $Name }
            } else {
                $errorOutput = $swaDeployOutput -join '`n'
                Write-ColorOutput Yellow "   WARNING: SWA CLI deploy failed. Creating resource first..."
                Write-Log "SWA CLI deploy failed, creating resource first: $errorOutput" "WARN"
                $result.Error = $errorOutput
            }
        }
        finally {
            # Clean up temp directory
            if (Test-Path $tempDeployDir) {
                Remove-Item -Path $tempDeployDir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }
    catch {
        $result.Error = $_.Exception.Message
        Write-Log "SWA CLI creation failed: $($_.Exception.Message)" "ERROR"
    }
    
    return $result
}

function New-StaticWebAppWithAzureCli {
    <#
    .SYNOPSIS
    Creates a Static Web App using Azure CLI and connects via REST API.
    #>
    param(
        [string]$Name,
        [string]$ResourceGroup,
        [string]$Location,
        [string]$SubscriptionId,
        [string]$RepositoryOwner,
        [string]$RepositoryName,
        [string]$Branch,
        [string]$AppLocation,
        [string]$ApiLocation,
        [string]$OutputLocation,
        [string]$GitHubPat,
        [switch]$Verbose
    )
    
    $result = @{
        Success = $false
        StaticWebApp = $null
        Error = ""
    }
    
    try {
        # Static Web Apps have limited region support
        $swaLocation = Get-StaticWebAppFallbackRegion -PreferredRegion $Location
        
        if (-not (Test-StaticWebAppRegion -Region $Location)) {
            Write-ColorOutput Yellow "WARNING: Static Web Apps not available in $Location"
            $supportedRegions = Get-StaticWebAppSupportedRegions
            Write-ColorOutput Yellow "   Available regions: $($supportedRegions -join ', ')"
            Write-ColorOutput Cyan "   Creating Static Web App in '$swaLocation' (closest supported region)"
            Write-Log "Static Web Apps not available in $Location, using $swaLocation" "WARN"
        }
        
        # Try creating with Azure CLI
        $swaCreateOutput = az staticwebapp create `
            --name $Name `
            --resource-group $ResourceGroup `
            --location $swaLocation `
            --sku Free `
            --output json 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-ColorOutput Green "   SUCCESS: Resource created in $swaLocation. Connecting to GitHub..."
            
            # Connect with REST API using helper function
            if ($GitHubPat -and $RepositoryOwner -and $RepositoryName) {
                $connectResult = Connect-StaticWebAppToGitHub `
                    -StaticWebAppName $Name `
                    -ResourceGroup $ResourceGroup `
                    -SubscriptionId $SubscriptionId `
                    -RepositoryOwner $RepositoryOwner `
                    -RepositoryName $RepositoryName `
                    -Branch $Branch `
                    -AppLocation $AppLocation `
                    -ApiLocation $ApiLocation `
                    -OutputLocation $OutputLocation `
                    -GitHubPat $GitHubPat
                
                if ($connectResult.Success) {
                    Write-Host "[OK]" -ForegroundColor Green
                    Write-ColorOutput Green "SUCCESS: Static Web App created and connected to GitHub!"
                    Write-Log "Static Web App created and connected via REST API" "INFO"
                    $result.Success = $true
                    $result.StaticWebApp = @{ name = $Name }
                } else {
                    Write-ColorOutput Yellow "   WARNING: Failed to connect to GitHub: $($connectResult.Error)"
                    Write-Log "Failed to connect GitHub: $($connectResult.Error)" "WARN"
                    $result.Error = $connectResult.Error
                }
            } else {
                Write-ColorOutput Yellow "   WARNING: Missing GitHub PAT or repository info."
                $result.Error = "Missing GitHub PAT or repository info"
            }
        } else {
            $errorMsg = $swaCreateOutput -join '`n'
            
            # Check if it's a region error
            if ($errorMsg -match "LocationNotAvailableForResourceType") {
                Write-Host "[X]" -ForegroundColor Red
                Write-ColorOutput Red "   ERROR: Static Web Apps are not available in $swaLocation."
                Write-ColorOutput Yellow "   Error: $errorMsg"
                Write-Log "Static Web App creation failed in ${swaLocation}: $errorMsg" "ERROR"
            } else {
                Write-Host "[X]" -ForegroundColor Red
                Write-ColorOutput Yellow "   WARNING: Static Web App creation failed."
                Write-ColorOutput Yellow "   Error: $errorMsg"
                Write-Log "Static Web App creation failed: $errorMsg" "ERROR"
            }
            $result.Error = $errorMsg
        }
    }
    catch {
        $result.Error = $_.Exception.Message
        Write-Log "Azure CLI SWA creation failed: $($_.Exception.Message)" "ERROR"
    }
    
    return $result
}

Export-ModuleMember -Function New-StaticWebAppWithGitHub, New-StaticWebAppWithSwaCli, New-StaticWebAppWithAzureCli

